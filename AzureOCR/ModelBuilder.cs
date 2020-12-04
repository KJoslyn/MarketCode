using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;

namespace AzureOCR
{
    public abstract class ModelBuilder<T> : IModelBuilder
    {
        public IEnumerable<T> CreateModels(IEnumerable<Line> lines)
        {
            List<T> models = new List<T>();
            foreach (Line line in lines)
            {
                foreach (Word word in line.Words)
                {
                    TakeNextWord(word);
                    if (Done)
                    {
                        T obj = BuildAndReset();
                        models.Add(obj);
                    }
                }
            }
            return models;
        }

        protected abstract bool Done { get; }

        protected abstract void TakeNextWord(Word word);

        protected T BuildAndReset()
        {
            if (!Done)
            {
                throw new ModelBuilderException("Build() called too early!", this);
            }
            T obj = Build();
            Reset();
            return obj;
        }

        protected abstract T Build();
        protected abstract void FinishBuildLevel();
        protected abstract void Reset();
    }
}
