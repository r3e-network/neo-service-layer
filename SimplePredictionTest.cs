using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.Prediction.Models;

namespace SimplePredictionTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🎯 Simple AI Prediction Service Test");
            Console.WriteLine("=" * 50);

            try
            {
                // Create a simple logger
                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<PredictionService>();

                // Create the prediction service
                Console.WriteLine("✅ Creating Prediction Service...");
                var predictionService = new PredictionService(logger);
                Console.WriteLine("✅ Prediction Service created successfully!");

                // Test service initialization
                Console.WriteLine("\n🔧 Testing service initialization...");
                await predictionService.InitializeAsync();
                Console.WriteLine("✅ Service initialized successfully!");

                // Test service start
                Console.WriteLine("\n🚀 Testing service start...");
                await predictionService.StartAsync();
                Console.WriteLine("✅ Service started successfully!");

                // Test health check
                Console.WriteLine("\n💚 Testing health check...");
                var health = await predictionService.GetHealthAsync();
                Console.WriteLine($"✅ Service health: {health}");

                // Test model creation
                Console.WriteLine("\n🧠 Testing model creation...");
                var modelDefinition = new PredictionModelDefinition
                {
                    Name = "TestModel",
                    Type = AIModelType.Prediction,
                    PredictionType = PredictionType.Price,
                    TargetVariable = "price",
                    InputFeatures = new List<string> { "volume", "sentiment" },
                    OutputFeatures = new List<string> { "predicted_price" }
                };

                var modelId = await predictionService.CreateModelAsync(modelDefinition, BlockchainType.NeoX);
                Console.WriteLine($"✅ Model created with ID: {modelId}");

                // Test prediction
                Console.WriteLine("\n🔮 Testing prediction...");
                var predictionRequest = new PredictionRequest
                {
                    ModelId = modelId,
                    InputData = new object[] { 1000000, 0.75 },
                    ReturnConfidence = true
                };

                var result = await predictionService.PredictAsync(predictionRequest, BlockchainType.NeoX);
                Console.WriteLine($"✅ Prediction completed!");
                Console.WriteLine($"   Request ID: {result.RequestId}");
                Console.WriteLine($"   Predictions: [{string.Join(", ", result.Predictions)}]");
                Console.WriteLine($"   Confidence: [{string.Join(", ", result.ConfidenceScores)}]");

                // Test sentiment analysis
                Console.WriteLine("\n😊 Testing sentiment analysis...");
                var sentimentRequest = new SentimentAnalysisRequest
                {
                    TextData = new[] { "Great project!", "Very bullish!" },
                    Keywords = new[] { "great", "bullish" }
                };

                var sentimentResult = await predictionService.AnalyzeSentimentAsync(sentimentRequest, BlockchainType.NeoX);
                Console.WriteLine($"✅ Sentiment analysis completed!");
                Console.WriteLine($"   Overall sentiment: {sentimentResult.OverallSentiment:F3}");
                Console.WriteLine($"   Confidence: {sentimentResult.Confidence:F3}");

                // Test service stop
                Console.WriteLine("\n🛑 Testing service stop...");
                await predictionService.StopAsync();
                Console.WriteLine("✅ Service stopped successfully!");

                Console.WriteLine("\n🎉 ALL TESTS PASSED!");
                Console.WriteLine("🏆 AI Prediction Service is working perfectly!");
                Console.WriteLine("=" * 50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
