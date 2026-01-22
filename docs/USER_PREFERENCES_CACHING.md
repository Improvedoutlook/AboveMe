# User Preferences Caching Implementation

## Overview
The AboveMe app now includes persistent caching for user location preferences using browser localStorage. This allows users' selections to remain saved across browser sessions, eliminating the need to re-enter their location information each time they visit the app.

## Implementation Details

### Components Added

1. **LocalStorageService** (`Services/LocalStorageService.cs`)
   - Provides a clean interface for interacting with browser localStorage
   - Handles JSON serialization/deserialization of user preferences
   - Includes error handling for robust operation
   - Methods:
     - `SetItemAsync<T>`: Save a value to localStorage
     - `GetItemAsync<T>`: Retrieve a value from localStorage
     - `RemoveItemAsync`: Remove a specific item
     - `ClearAsync`: Clear all localStorage data

2. **UserLocationPreferences Model** (defined in `LocalStorageService.cs`)
   - Stores all user location-related preferences:
     - Country
     - City
     - Timezone
     - Latitude
     - Longitude
     - LastUpdated timestamp

### How It Works

1. **On Application Load** (`OnInitializedAsync`):
   - The app attempts to load saved preferences from localStorage
   - If preferences exist, they are automatically applied to the form fields
   - If no saved preferences exist, the form starts empty

2. **When User Makes Selections**:
   - Country dropdown (`@bind:after="OnCountryChanged"`)
   - Timezone dropdown (`@bind:after="OnTimezoneChanged"`)
   - City dropdown (`@bind:after="OnCityChanged"`)
   - Share location button (`OnShareLocationClicked`)
   - Each of these triggers `SaveUserPreferencesAsync()` to persist the current state

3. **Data Persistence**:
   - Preferences are stored in browser localStorage with the key `"userLocationPreferences"`
   - Data persists across:
     - Page refreshes
     - Browser restarts
     - Multiple visits to the site
   - Data is stored as JSON for easy serialization

### Storage Duration

The preferences will remain cached **indefinitely** unless:
- The user clears their browser data/cache
- The user uses a different browser or device
- The app explicitly clears localStorage (not currently implemented)
- The user uses incognito/private browsing mode (which uses separate, temporary storage)

### Privacy & Security Notes

- All data is stored **locally** in the user's browser only
- No preferences are sent to any server
- Each browser on each device maintains its own separate cache
- Users can manually clear their browser's localStorage to reset preferences

### Testing

To test the implementation:

1. Open the app in a browser
2. Select a Country, Timezone, and/or City
3. OR use the "Share my location" button
4. Refresh the page (F5 or Ctrl+R)
5. Verify that your selections are automatically restored
6. Close the browser completely and reopen it
7. Navigate back to the app
8. Verify selections are still present

### Browser Compatibility

localStorage is supported in all modern browsers:
- Chrome/Edge (all versions)
- Firefox (all versions)
- Safari (all versions)
- Opera (all versions)

### Future Enhancements

Possible improvements:
1. Add a "Clear Preferences" button for users
2. Add expiration logic (e.g., clear preferences after 30 days)
3. Store additional preferences (favorite Webb images, preferred data views, etc.)
4. Add versioning to handle future changes to the preference structure
5. Implement migration logic for old preference formats

## Code Changes Summary

### Files Modified:
1. **Program.cs**: Registered `LocalStorageService` with dependency injection
2. **Home.razor**: 
   - Added `@using AboveMe.Services`
   - Injected `LocalStorageService`
   - Added `LoadUserPreferencesAsync()` method
   - Added `SaveUserPreferencesAsync()` method
   - Added event handlers for dropdown changes
   - Modified `OnInitializedAsync()` to load saved preferences
   - Modified `OnShareLocationClicked()` to save preferences

### Files Created:
1. **Services/LocalStorageService.cs**: New service for localStorage operations

## Developer Notes

- The service uses asynchronous operations for all localStorage interactions to prevent UI blocking
- Console logging is included for debugging (can be removed in production)
- Error handling ensures the app continues to function even if localStorage operations fail
- The implementation follows Blazor best practices for dependency injection and component lifecycle
