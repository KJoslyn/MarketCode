# OptionsTradingBot

This is an automated options-trading bot built using .NET Core and .NET 5.0. It is designed to constantly monitor a live video feed that displays current options positions held by a recommendation service (AKA the "live portfolio"), and respond to changes detected in the portfolio. The user has the ability to specify a number of parameters that dictate how the bot should respond when a change in the portfolio is detected. The main logic that decides what orders should be placed  exists in [OrderMangager.cs](https://github.com/KJoslyn/MarketCode/blob/main/Core/OrderManager.cs) and uses parameters specified in an [OrderConfig](https://github.com/KJoslyn/MarketCode/blob/main/Core/OrderConfig.cs). The basic premise of the bot is to mimic the changes in the live portfolio by placing identical trades that are scaled to the user's own portfolio size and desires.

## The Live Portfolio

This bot was designed to be easily adaptable to work with any live portfolio. The particular live portfolio used in the creation of this bot is made available to subscribers of [RagingBull](https://ragingbull.com)'s LottoX service. A screenshot of the live portfolio taken in October 2020 looks like this:

![LottoX portfolio](https://github.com/KJoslyn/MarketCode/blob/main/LottoX.PNG)

Note in particular the left column of the screen, which displays the current options positions in the portfolio. It includes information such as the quantity of the option being held, the last price of the option (most recently traded price), etc. The middle column of the screen displays all live trades performed by the recommender. This information is disregarded because not all trades are relevant to the LottoX service.

## Capturing Screenshots using PupeteerSharp

In order to constantly monitor the live portfolio, the bot spins up an instance of a [PupeteerSharp](https://www.puppeteersharp.com) headless browser, which logs in directly to the live portfolio page. Every 15 seconds, the browser instance takes a screenshot of the live portfolio and crops it to include only the relevant information in the left-hand column. Relevant methods for taking screenshots exist in [LottoXClient.cs](https://github.com/KJoslyn/MarketCode/blob/main/LottoXService/LottoXClient.cs). Before passing the screenshot on to the image recognition component, a simple comparison is made to the previous screenshot using an [ImageConsistencyClient](https://github.com/KJoslyn/MarketCode/blob/b5bfb0e9f57dc2e10b330980c87604723257debe/AzureOCR/ImageConsistencyClient.cs) to determine if anything has changed.

## Position Detection Using Azure's OCR API

If a change in the positions is detected, [Microsoft Azure's Computer Vision API](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) is employed to extract the relevant text. The [ImageToModelsClient](https://github.com/KJoslyn/MarketCode/blob/b5bfb0e9f57dc2e10b330980c87604723257debe/AzureOCR/ImageToModelsClient.cs) is responsible for making the calls to Azure's ComputerVisionClient and parsing and converting the text into Position models. These models are built using a builder pattern, whereby each field of the Position model is set sequentially depending on the build level of the PositionBuilder object (_**** see_ [_line 34 of the Position builder_](https://github.com/KJoslyn/MarketCode/blob/b5bfb0e9f57dc2e10b330980c87604723257debe/LottoXService/Builders/PositionBuilder.cs#L34)).

## Position Validation

## Calculating changes in positions using NoSql database LiteDB

## Determining action using an Order Manager

## Placing Live or Paper Trades using TD Ameritrade's API

