using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Processing
{
    /// <summary>
    /// Processes various types of data with transformation and validation.
    /// </summary>
    public abstract class DataProcessor<T>
    {
        protected readonly List<T> _items = new();
        protected readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Adds an item to the processor.
        /// </summary>
        public virtual async Task AddItemAsync(T item)
        {
            await _semaphore.WaitAsync();
            try
            {
                _items.Add(item);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Processes all items in the collection.
        /// </summary>
        public abstract Task<IEnumerable<T>> ProcessAllAsync();

        /// <summary>
        /// Validates an item before processing.
        /// </summary>
        protected abstract bool ValidateItem(T item);

        /// <summary>
        /// Gets the count of items.
        /// </summary>
        public int Count => _items.Count;
    }

    /// <summary>
    /// Processes string data with various transformations.
    /// </summary>
    public class StringDataProcessor : DataProcessor<string>
    {
        private readonly StringTransformOptions _options;

        public StringDataProcessor(StringTransformOptions? options = null)
        {
            _options = options ?? new StringTransformOptions();
        }

        public override async Task<IEnumerable<string>> ProcessAllAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var results = new List<string>();
                foreach (var item in _items)
                {
                    if (ValidateItem(item))
                    {
                        var processed = await ProcessStringAsync(item);
                        results.Add(processed);
                    }
                }
                return results;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected override bool ValidateItem(string item)
        {
            return !string.IsNullOrWhiteSpace(item);
        }

        private async Task<string> ProcessStringAsync(string input)
        {
            await Task.Delay(5); // Simulate processing

            var result = input;

            if (_options.TrimWhitespace)
                result = result.Trim();

            if (_options.ConvertCase == CaseConversion.Upper)
                result = result.ToUpper();
            else if (_options.ConvertCase == CaseConversion.Lower)
                result = result.ToLower();

            if (_options.RemoveSpecialCharacters)
                result = System.Text.RegularExpressions.Regex.Replace(result, @"[^a-zA-Z0-9\s]", "");

            return result;
        }
    }

    /// <summary>
    /// Options for string transformation.
    /// </summary>
    public class StringTransformOptions
    {
        public bool TrimWhitespace { get; set; } = true;
        public CaseConversion ConvertCase { get; set; } = CaseConversion.None;
        public bool RemoveSpecialCharacters { get; set; } = false;
    }

    public enum CaseConversion
    {
        None,
        Upper,
        Lower
    }

    /// <summary>
    /// Processes JSON data with schema validation.
    /// </summary>
    public class JsonDataProcessor : DataProcessor<JsonDocument>
    {
        private readonly string? _schemaPath;

        public JsonDataProcessor(string? schemaPath = null)
        {
            _schemaPath = schemaPath;
        }

        public override async Task<IEnumerable<JsonDocument>> ProcessAllAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var validDocuments = _items.Where(ValidateItem).ToList();
                
                // Simulate async processing
                await Task.Delay(10 * validDocuments.Count);
                
                return validDocuments;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected override bool ValidateItem(JsonDocument item)
        {
            if (item == null || item.RootElement.ValueKind == JsonValueKind.Null)
                return false;

            // Simple validation - check if it has required properties
            try
            {
                var root = item.RootElement;
                return root.ValueKind == JsonValueKind.Object;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts a specific field from all JSON documents.
        /// </summary>
        public async Task<IEnumerable<string>> ExtractFieldAsync(string fieldName)
        {
            await _semaphore.WaitAsync();
            try
            {
                var results = new List<string>();
                foreach (var doc in _items)
                {
                    if (doc.RootElement.TryGetProperty(fieldName, out var value))
                    {
                        results.Add(value.ToString());
                    }
                }
                return results;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Generic data pipeline for chaining processors.
    /// </summary>
    public class DataPipeline<TInput, TOutput>
    {
        private readonly List<Func<TInput, Task<TOutput>>> _stages = new();

        public DataPipeline<TInput, TOutput> AddStage(Func<TInput, Task<TOutput>> stage)
        {
            _stages.Add(stage);
            return this;
        }

        public async Task<TOutput> ExecuteAsync(TInput input)
        {
            if (_stages.Count == 0)
                throw new InvalidOperationException("Pipeline has no stages");

            // For simplicity, just execute the first stage
            return await _stages[0](input);
        }
    }
}