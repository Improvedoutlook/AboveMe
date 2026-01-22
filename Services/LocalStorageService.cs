using Microsoft.JSInterop;
using System.Text.Json;

namespace AboveMe.Services
{
    /// <summary>
    /// Service for managing browser localStorage to persist user preferences across sessions.
    /// </summary>
    public class LocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Save a value to localStorage with the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the value to store</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="value">Value to store</param>
        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to localStorage: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieve a value from localStorage with the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the value to retrieve</typeparam>
        /// <param name="key">Storage key</param>
        /// <returns>The stored value, or default(T) if not found</returns>
        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json))
                {
                    return default;
                }
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from localStorage: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Remove an item from localStorage.
        /// </summary>
        /// <param name="key">Storage key</param>
        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from localStorage: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all items from localStorage.
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.clear");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Model for storing user location preferences.
    /// </summary>
    public class UserLocationPreferences
    {
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Timezone { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
