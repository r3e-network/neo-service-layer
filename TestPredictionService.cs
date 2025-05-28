using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.Prediction.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;

namespace TestPredictionService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🎯 Testing Neo Service Layer AI Prediction Service");
            Console.WriteLine(new string('=', 60));

            // Create a host builder with dependency injection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder => builder.AddConsole());
                    services.AddSingleton<NeoServiceLayer.AI.Prediction.IPredictionService, PredictionService>();
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var predictionService = host.Services.GetRequiredService<NeoServiceLayer.AI.Prediction.IPredictionService>();

            try
            {
                Console.WriteLine("✅ Successfully created Prediction Service instance");

                // Test 1: Initialize the service
                Console.WriteLine("\n🔧 Test 1: Initializing Prediction Service...");
                await predictionService.InitializeAsync();
                Console.WriteLine("✅ Service initialized successfully");

                // Test 2: Start the service
                Console.WriteLine("\n🚀 Test 2: Starting Prediction Service...");
                await predictionService.StartAsync();
                Console.WriteLine("✅ Service started successfully");

                // Test 3: Check service health
                Console.WriteLine("\n💚 Test 3: Checking Service Health...");
                var health = await predictionService.GetHealthAsync();
                Console.WriteLine($"✅ Service Health: {health}");

                // Test 4: Create a prediction model
                Console.WriteLine("\n🧠 Test 4: Creating Prediction Model...");
                var modelDefinition = new PredictionModelDefinition
                {
                    Name = "TestCryptoPriceModel",
                    Type = AIModelType.Prediction,
                    PredictionType = PredictionType.Price,
                    TargetVariable = "price",
                    InputFeatures = new List<string> { "volume", "market_cap", "sentiment" },
                    OutputFeatures = new List<string> { "predicted_price", "confidence" }
                };

                var modelId = await predictionService.CreateModelAsync(modelDefinition, BlockchainType.NeoX);
                Console.WriteLine($"✅ Created model with ID: {modelId}");

                // Test 5: Make a prediction
                Console.WriteLine("\n🔮 Test 5: Making Prediction...");
                var predictionRequest = new PredictionRequest
                {
                    ModelId = modelId,
                    InputData = new object[] { 1000000, 50000000, 0.75 }, // volume, market_cap, sentiment
                    Parameters = new Dictionary<string, object> { ["confidence_threshold"] = 0.8 },
                    ReturnConfidence = true
                };

                var predictionResult = await predictionService.PredictAsync(predictionRequest, BlockchainType.NeoX);
                Console.WriteLine($"✅ Prediction completed:");
                Console.WriteLine($"   Request ID: {predictionResult.RequestId}");
                Console.WriteLine($"   Model ID: {predictionResult.ModelId}");
                Console.WriteLine($"   Predictions: [{string.Join(", ", predictionResult.Predictions)}]");
                Console.WriteLine($"   Confidence Scores: [{string.Join(", ", predictionResult.ConfidenceScores)}]");
                Console.WriteLine($"   Processed At: {predictionResult.ProcessedAt}");

                // Test 6: Sentiment Analysis
                Console.WriteLine("\n😊 Test 6: Sentiment Analysis...");
                var sentimentRequest = new NeoServiceLayer.Core.SentimentAnalysisRequest
                {
                    TextData = new[] { "Bitcoin is going to the moon!", "The market looks very bullish today", "Great investment opportunity" },
                    Keywords = new[] { "bitcoin", "bullish", "investment" },
                    Language = "en"
                };

                var sentimentResult = await predictionService.AnalyzeSentimentAsync(sentimentRequest, BlockchainType.NeoX);
                Console.WriteLine($"✅ Sentiment Analysis completed:");
                Console.WriteLine($"   Analysis ID: {sentimentResult.AnalysisId}");
                Console.WriteLine($"   Overall Sentiment: {sentimentResult.OverallSentiment:F3}");
                Console.WriteLine($"   Confidence: {sentimentResult.Confidence:F3}");
                Console.WriteLine($"   Sample Size: {sentimentResult.SampleSize}");
                Console.WriteLine($"   Keywords: {string.Join(", ", sentimentResult.KeywordSentiments.Keys)}");

                // Test 7: Get available models
                Console.WriteLine("\n📋 Test 7: Getting Available Models...");
                var models = await predictionService.GetModelsAsync(BlockchainType.NeoX);
                Console.WriteLine($"✅ Found {models.Count()} available models");
                foreach (var model in models)
                {
                    Console.WriteLine($"   - {model.Name} (ID: {model.Id}, Type: {model.PredictionType})");
                }

                // Test 8: Stop the service
                Console.WriteLine("\n🛑 Test 8: Stopping Prediction Service...");
                await predictionService.StopAsync();
                Console.WriteLine("✅ Service stopped successfully");

                Console.WriteLine("\n🎉 ALL TESTS PASSED! AI Prediction Service is working perfectly!");
                Console.WriteLine(new string('=', 60));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                logger.LogError(ex, "Test execution failed");
            }
        }
    }
}
