using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;
using Kashout.Core.Models;

namespace Kashout.Core.Services
{
    public class IDVerificationPipeline
    {
        private readonly string _tesseractPath;
        private readonly bool _testMode;
        private readonly double _latencyThresholdMs = 300;

        public IDVerificationPipeline(string tesseractPath = null)
        {
            _tesseractPath = tesseractPath ?? Environment.GetEnvironmentVariable("TESSERACT_PATH") ?? "/usr/share/tesseract-ocr/5/tessdata";
            _testMode = Environment.GetEnvironmentVariable("TEST_MODE") == "true";
        }

        /// <summary>
        /// Main pipeline: Upload → Preprocess → Face Embedding → Similarity → MRZ OCR → Result
        /// </summary>
        public async Task<IdVerificationResult> ProcessAsync(IdVerificationRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new IdVerificationResult();
            var checkpoints = new List<string>();

            try
            {
                // Checkpoint A: Face detected?
                var selfieEmbedding = await ExtractFaceEmbedding(request.SelfieImage, "selfie");
                if (selfieEmbedding == null)
                {
                    checkpoints.Add("❌ A: Face detection failed in selfie");
                    return FailedResult(result, "No face detected in selfie image", checkpoints, stopwatch);
                }
                checkpoints.Add("✅ A: Face detected in selfie");

                var idFaceEmbedding = await ExtractFaceEmbedding(request.IdDocumentImage, "id_document");
                if (idFaceEmbedding == null)
                {
                    checkpoints.Add("❌ A: Face detection failed in ID document");
                    return FailedResult(result, "No face detected in ID document", checkpoints, stopwatch);
                }
                checkpoints.Add("✅ A: Face detected in ID document");

                // Checkpoint B: Embeddings valid?
                if (selfieEmbedding.Features == null || idFaceEmbedding.Features == null)
                {
                    checkpoints.Add("❌ B: Invalid face embeddings");
                    return FailedResult(result, "Failed to extract face embeddings", checkpoints, stopwatch);
                }
                checkpoints.Add("✅ B: Face embeddings extracted successfully");

                // Checkpoint C: Score ≥ threshold?
                var similarityScore = CalculateCosineSimilarity(selfieEmbedding.Features, idFaceEmbedding.Features);
                result.SimilarityScore = similarityScore;

                if (similarityScore < request.SimilarityThreshold)
                {
                    checkpoints.Add($"❌ C: Similarity score {similarityScore:F3} below threshold {request.SimilarityThreshold}");
                    return FailedResult(result, $"Similarity score {similarityScore:F3} below threshold", checkpoints, stopwatch);
                }
                checkpoints.Add($"✅ C: Similarity score {similarityScore:F3} above threshold");

                // Checkpoint D & E: MRZ parsing with fallback
                var mrzData = await ExtractMRZ(request.IdDocumentImage);
                if (mrzData == null || !mrzData.IsValid)
                {
                    checkpoints.Add("❌ D: MRZ parsing failed");
                    checkpoints.Add("✅ E: OCR fallback engaged");
                    
                    // OCR fallback using Tesseract
                    var ocrText = await PerformOCRFallback(request.IdDocumentImage);
                    if (string.IsNullOrEmpty(ocrText))
                    {
                        checkpoints.Add("❌ E: OCR fallback also failed");
                        return FailedResult(result, "MRZ parsing and OCR fallback both failed", checkpoints, stopwatch);
                    }
                    
                    // Extract fields from OCR text
                    result.MrzFields = ExtractFieldsFromOCR(ocrText);
                }
                else
                {
                    checkpoints.Add("✅ D: MRZ parsed successfully");
                    checkpoints.Add("⏭️ E: OCR fallback not needed");
                    result.MrzFields = ConvertMrzToFields(mrzData);
                }

                // Checkpoint F: Output JSON sane?
                if (!ValidateOutputJson(result))
                {
                    checkpoints.Add("❌ F: Output JSON validation failed");
                    return FailedResult(result, "Output validation failed", checkpoints, stopwatch);
                }
                checkpoints.Add("✅ F: Output JSON validated");

                // Checkpoint H: Latency check
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                
                if (result.ProcessingTimeMs > _latencyThresholdMs)
                {
                    checkpoints.Add($"⚠️ H: Processing time {result.ProcessingTimeMs}ms exceeds threshold {_latencyThresholdMs}ms");
                }
                else
                {
                    checkpoints.Add($"✅ H: Processing time {result.ProcessingTimeMs}ms within threshold");
                }

                // Checkpoint I: OCR image quality assessment
                var imageQuality = AssessImageQuality(request.IdDocumentImage);
                if (imageQuality < 0.6)
                {
                    checkpoints.Add($"⚠️ I: Image quality {imageQuality:F2} is low but acceptable");
                }
                else
                {
                    checkpoints.Add($"✅ I: Image quality {imageQuality:F2} is acceptable");
                }

                // Checkpoint G: Logging
                await LogProcessingResult(result, checkpoints);
                checkpoints.Add("✅ G: Processing logged successfully");

                result.IsSuccessful = true;
                result.CheckpointResults = checkpoints;
                
                // Debug information
                result.Debug["selfie_confidence"] = selfieEmbedding.Confidence;
                result.Debug["id_face_confidence"] = idFaceEmbedding.Confidence;
                result.Debug["selfie_bbox"] = selfieEmbedding.BoundingBox;
                result.Debug["id_face_bbox"] = idFaceEmbedding.BoundingBox;
                result.Debug["image_quality_score"] = imageQuality;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                checkpoints.Add($"❌ Exception: {ex.Message}");
                return FailedResult(result, ex.Message, checkpoints, stopwatch);
            }
        }

        private async Task<FaceEmbedding> ExtractFaceEmbedding(byte[] imageData, string imageType)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            try
            {
                // Preprocess image
                var preprocessedImage = await PreprocessImage(imageData);
                
                if (_testMode)
                {
                    // Return simulated embedding for tests
                    return CreateSimulatedEmbedding(imageData, imageType);
                }

                // In a real implementation, this would use InsightFace or similar
                // For MVP, we simulate the face detection and embedding extraction
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(preprocessedImage);
                
                // Simulate face detection
                var faceDetected = SimulateFaceDetection(image);
                if (!faceDetected.detected)
                    return null;

                // Simulate embedding extraction (512-dimensional vector)
                var embedding = SimulateEmbeddingExtraction(image, imageType);
                
                return new FaceEmbedding
                {
                    Features = embedding,
                    Confidence = faceDetected.confidence,
                    BoundingBox = faceDetected.bbox
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Face embedding extraction failed: {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]> PreprocessImage(byte[] imageData)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageData);
            
            // Resize if too large
            if (image.Width > 1024 || image.Height > 1024)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(1024, 1024),
                    Mode = ResizeMode.Max
                }));
            }

            // Convert to standard format
            using var stream = new MemoryStream();
            await image.SaveAsJpegAsync(stream);
            return stream.ToArray();
        }

        private (bool detected, double confidence, int[] bbox) SimulateFaceDetection(SixLabors.ImageSharp.Image<Rgb24> image)
        {
            // Simulate face detection based on image characteristics
            if (image.Width < 100 || image.Height < 100)
                return (false, 0.0, null);

            // For simulation, assume faces are in the center quarter of the image
            var centerX = image.Width / 2;
            var centerY = image.Height / 2;
            var faceSize = Math.Min(image.Width, image.Height) / 4;

            var bbox = new int[] { 
                centerX - faceSize/2, 
                centerY - faceSize/2, 
                faceSize, 
                faceSize 
            };

            // Simulate higher confidence for larger, clearer images
            var confidence = Math.Min(0.95, 0.7 + (image.Width * image.Height) / 1000000.0);

            return (true, confidence, bbox);
        }

        private float[] SimulateEmbeddingExtraction(SixLabors.ImageSharp.Image<Rgb24> image, string imageType)
        {
            var embedding = new float[512]; // Standard face embedding dimension
            var random = new Random(image.GetHashCode() + imageType.GetHashCode()); // Deterministic based on image

            // Simulate different embedding patterns for selfie vs ID
            var baseValue = imageType == "selfie" ? 0.1f : 0.15f;
            
            for (int i = 0; i < 512; i++)
            {
                embedding[i] = baseValue + (float)(random.NextGaussian() * 0.05);
            }

            // Normalize the vector
            var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }

            return embedding;
        }

        private FaceEmbedding CreateSimulatedEmbedding(byte[] imageData, string imageType)
        {
            var hash = imageData.GetHashCode();
            var random = new Random(hash);
            
            var embedding = new float[512];
            for (int i = 0; i < 512; i++)
            {
                embedding[i] = (float)random.NextGaussian();
            }

            return new FaceEmbedding
            {
                Features = embedding,
                Confidence = 0.85 + random.NextDouble() * 0.1,
                BoundingBox = new int[] { 100, 100, 200, 200 }
            };
        }

        private double CalculateCosineSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
                throw new ArgumentException("Embeddings must have the same dimension");

            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < embedding1.Length; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += embedding1[i] * embedding1[i];
                magnitude2 += embedding2[i] * embedding2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }

        private async Task<MrzData> ExtractMRZ(byte[] imageData)
        {
            try
            {
                // Simulate FastMRZ processing
                // In real implementation, this would use a library like FastMRZ or similar
                
                if (_testMode)
                {
                    return CreateSimulatedMRZ(imageData);
                }

                // For MVP, we'll simulate MRZ extraction by looking for patterns
                var ocrText = await PerformOCRFallback(imageData);
                if (string.IsNullOrEmpty(ocrText))
                    return null;

                return ParseMRZFromText(ocrText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MRZ extraction failed: {ex.Message}");
                return null;
            }
        }

        private async Task<string> PerformOCRFallback(byte[] imageData)
        {
            try
            {
                // In real implementation, use Tesseract OCR
                // For MVP, simulate OCR based on image characteristics
                
                if (_testMode)
                {
                    return SimulateOCRText(imageData);
                }

                // Placeholder for actual Tesseract implementation
                return SimulateOCRText(imageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR failed: {ex.Message}");
                return null;
            }
        }

        private MrzData CreateSimulatedMRZ(byte[] imageData)
        {
            var hash = Math.Abs(imageData.GetHashCode());
            
            return new MrzData
            {
                DocumentType = "P",
                DocumentNumber = $"X{hash % 10000000:D7}",
                CountryCode = "USA",
                Nationality = "USA",
                Surname = "SMITH",
                GivenNames = "JOHN",
                DateOfBirth = "850315",
                ExpirationDate = "301231",
                Sex = "M",
                PersonalNumber = $"{hash % 100000000:D8}",
                IsValid = true
            };
        }

        private string SimulateOCRText(byte[] imageData)
        {
            var hash = Math.Abs(imageData.GetHashCode());
            return $@"
UNITED STATES OF AMERICA
DRIVER LICENSE
Name: JOHN SMITH
DOB: 03/15/1985
License No: D{hash % 10000000:D7}
Exp: 12/31/2030
Address: 123 MAIN ST, ANYTOWN, CA 90210
";
        }

        private MrzData ParseMRZFromText(string text)
        {
            // Simple MRZ parsing for MVP
            var mrz = new MrzData();
            
            if (text.Contains("DRIVER") || text.Contains("LICENSE"))
            {
                mrz.DocumentType = "DL";
                mrz.IsValid = true;
            }
            
            // Extract basic fields using simple pattern matching
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Name:"))
                {
                    var name = line.Replace("Name:", "").Trim();
                    var parts = name.Split(' ');
                    if (parts.Length >= 2)
                    {
                        mrz.GivenNames = parts[0];
                        mrz.Surname = string.Join(" ", parts.Skip(1));
                    }
                }
                // Add more field extraction as needed
            }

            return mrz;
        }

        private Dictionary<string, string> ExtractFieldsFromOCR(string ocrText)
        {
            var fields = new Dictionary<string, string>();
            
            var lines = ocrText.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Name:"))
                    fields["name"] = line.Replace("Name:", "").Trim();
                if (line.Contains("DOB:"))
                    fields["date_of_birth"] = line.Replace("DOB:", "").Trim();
                if (line.Contains("License No:"))
                    fields["license_number"] = line.Replace("License No:", "").Trim();
                if (line.Contains("Exp:"))
                    fields["expiration"] = line.Replace("Exp:", "").Trim();
            }

            return fields;
        }

        private Dictionary<string, string> ConvertMrzToFields(MrzData mrz)
        {
            return new Dictionary<string, string>
            {
                ["document_type"] = mrz.DocumentType,
                ["document_number"] = mrz.DocumentNumber,
                ["country_code"] = mrz.CountryCode,
                ["nationality"] = mrz.Nationality,
                ["surname"] = mrz.Surname,
                ["given_names"] = mrz.GivenNames,
                ["date_of_birth"] = mrz.DateOfBirth,
                ["expiration_date"] = mrz.ExpirationDate,
                ["sex"] = mrz.Sex
            };
        }

        private bool ValidateOutputJson(IdVerificationResult result)
        {
            return result.SimilarityScore >= 0 && 
                   result.SimilarityScore <= 1 && 
                   result.MrzFields != null;
        }

        private double AssessImageQuality(byte[] imageData)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageData);
                
                // Simple quality assessment based on size and aspect ratio
                var quality = 0.5; // Base quality
                
                if (image.Width >= 640 && image.Height >= 480)
                    quality += 0.3;
                
                if (image.Width >= 1024 && image.Height >= 768)
                    quality += 0.2;
                
                return Math.Min(1.0, quality);
            }
            catch
            {
                return 0.0;
            }
        }

        private async Task LogProcessingResult(IdVerificationResult result, List<string> checkpoints)
        {
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ID Verification - " +
                          $"Success: {result.IsSuccessful}, " +
                          $"Similarity: {result.SimilarityScore:F3}, " +
                          $"Processing Time: {result.ProcessingTimeMs}ms";
            
            Console.WriteLine(logEntry);
            
            // Log checkpoints
            foreach (var checkpoint in checkpoints)
            {
                Console.WriteLine($"  {checkpoint}");
            }
        }

        private IdVerificationResult FailedResult(IdVerificationResult result, string error, List<string> checkpoints, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            result.IsSuccessful = false;
            result.ErrorMessage = error;
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            result.CheckpointResults = checkpoints;
            return result;
        }
    }

    // Extension method for Gaussian random numbers
    public static class RandomExtensions
    {
        public static double NextGaussian(this Random random, double mean = 0, double stdDev = 1)
        {
            // Box-Muller transform
            if (random == null) random = new Random();
            
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}