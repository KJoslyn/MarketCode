# OptionsTradingBot

This is an automated options-trading bot built using .NET Core and .NET 5.0. It is designed to constantly monitor a live video feed that displays current options positions held by a recommendation service (AKA the "live portfolio"), and respond to changes detected in the portfolio. The user has the ability to specify a number of parameters that dictate how the bot should respond when a change in the portfolio is detected. These parameters are set using an appsettings.json file that is owned only by the user and is not to be shared publicly. The main logic that decides what orders should be placed  exists in [OrderMangager.cs](https://github.com/KJoslyn/MarketCode/blob/main/Core/OrderManager.cs) and uses parameters specified in an [OrderConfig](https://github.com/KJoslyn/MarketCode/blob/main/Core/OrderConfig.cs). The basic premise of the bot is to mimic the changes in the live portfolio by placing identical trades that are scaled to the user's own portfolio size and desires.

## The Live Portfolio

This bot was designed to be easily adaptable to work with any live portfolio. The particular live portfolio used in the creation of this bot is made available to subscribers of [RagingBull](https://ragingbull.com)'s LottoX service. A screenshot of the live portfolio taken in October 2020 looks like this:

![LottoX portfolio](https://github.com/KJoslyn/MarketCode/blob/main/LottoX.PNG)

Note in particular the left column of the screen, which displays the current options positions in the portfolio. It includes information such as the quantity of the option being held, the last price of the option (most recently traded price), etc. The middle column of the screen displays all live trades performed by the recommender. This information is disregarded because not all trades are relevant to the LottoX service.

## Capturing Screenshots using PupeteerSharp

In order to constantly monitor the live portfolio, the bot spins up an instance of a [PupeteerSharp](https://www.puppeteersharp.com) headless browser, which logs in directly to the live portfolio page. Every 15 seconds, the browser instance takes a screenshot of the live portfolio and crops it to include only the relevant information in the left-hand column. Relevant methods for taking screenshots exist in [LottoXClient.cs](https://github.com/KJoslyn/MarketCode/blob/main/LottoXService/LottoXClient.cs). Before passing the screenshot on to the image recognition component, a simple comparison is made to the previous screenshot using an [ImageConsistencyClient](https://github.com/KJoslyn/MarketCode/blob/b5bfb0e9f57dc2e10b330980c87604723257debe/AzureOCR/ImageConsistencyClient.cs) to determine if anything has changed.

## Position Detection Using Azure's OCR API

If a change in the positions is detected, [Microsoft Azure's Computer Vision API](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) is employed to extract the relevant text. The [ImageToModelsClient](https://github.com/KJoslyn/MarketCode/blob/b5bfb0e9f57dc2e10b330980c87604723257debe/AzureOCR/ImageToModelsClient.cs) is responsible for making the calls to Azure's ComputerVisionClient and parsing and converting the text into Position models. These models are built using a builder pattern, whereby each field of the Position model is set sequentially depending on the build level of the PositionBuilder object (_**** see_ [_line 34 of the Position builder_](https://github.com/KJoslyn/MarketCode/blob/b5bfb0e9f57dc2e10b330980c87604723257debe/LottoXService/Builders/PositionBuilder.cs#L34)).

## Position Validation

The image-to-text recognition is not always 100% accurate, so validation is required for option symbols and position quantities. Option symbols are validated by getting a live market quote (using a [MarketDataClient](https://github.com/KJoslyn/OptionsTradingBot/blob/c751f5fbd1bb6044747d6772c64226cebb63fa8f/Core/MarketDataClient.cs#L19) and comparing the quote price to the value in the "last" price column of the live portfolio screenshot. If an option symbol is misinterpreted, a hamming distance comparison is made to every previously used symbol (used symbols are stored in a NoSql database.) Candidate symbols (based on hamming distance) are also evaluated by getting live market quotes.

The "quantity" field is actually determined by dividing the "market value" field by the "last" price field, since these fields have not been observed to be misinterpreted, unlike the "quantity" field itself.

## Calculating changes in positions using NoSql database LiteDB

Whenever a position in the live portfolio changes, it is updated in a database. This implementation uses [LiteDB](https://www.litedb.org/), although any database implementation can be used. Before updating the position however, a "position delta" is computed by comparing the new or updated position to the positions stored in the database. This way, the bot can determine how many options contracts were bought or sold without sifting through the much larger (and mostly irrelevant) orders column.

## Determining actions using an Order Manager

The [Order Mangager](https://github.com/KJoslyn/MarketCode/blob/main/Core/OrderManager.cs) takes a time-sorted set of position deltas and uses parameters specified in an [OrderConfig](https://github.com/KJoslyn/MarketCode/blob/main/Core/OrderConfig.cs) to determine what orders, if any, the bot should place. The Order Manager will then create a list of Orders to send to the broker client.

## Placing Live or Paper Trades using TD Ameritrade's API

Given a list of Orders, the broker client will execute the corresponding trades. This broker client only needs to implement the [IBrokerClient](https://github.com/KJoslyn/OptionsTradingBot/blob/main/Core/IBrokerClient.cs) interface; thus, the application can be easily configured for either paper trading or real live trading using any brokerage, simply by modifying the [UsePaperTrade](https://github.com/KJoslyn/OptionsTradingBot/blob/c751f5fbd1bb6044747d6772c64226cebb63fa8f/LottoXService/GeneralConfig.cs#L6) field of the general configuration. In this repository, an implementation of IBrokerClient was developed for trading using a TD Ameritrade account. This implementation, called [TDClient, can be found here](https://github.com/KJoslyn/OptionsTradingBot/blob/main/TDAmeritrade/TDClient.cs). The paper trade version, which uses another instance of a LiteDB database to keep track of the user's positions, [can be found here](https://github.com/KJoslyn/OptionsTradingBot/blob/main/Core/PaperTradeBrokerClient.cs).

## Using Serilog and Seq for logging

The widely popular [Serilog](https://github.com/serilog/serilog) logging platform is incorporated into this project to provide an easy-to-use structured logging service. The log messages are passed to [Seq](https://datalust.co/seq) so that they can be easily searched (by position symbol, etc.), filtered and sorted. Seq also provides a very convenient method for sending email alerts when specific errors are encountered that require immediate attention.

## Using elmah.io to ensure application health

Although this feature is not complete, the options trading bot will use [elmah.io](https://elmah.io/) to monitor the application's health throughout the time it is running. It will accomplish this by using a client-server heartbeat mechanism. If a heartbeat is not received by the server, it will report an error, indicating that the application has unexpectedly frozen or crashed.
