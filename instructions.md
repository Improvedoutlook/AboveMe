# AboveMe Project - AI Agent Instructions

## Project Overview
AboveMe is a .NET Blazor WebAssembly application that provides an astronomy-themed experience. The app displays celestial data, features a starry twinkling background, and integrates with APIs like ipgeolocation and NASA's Images API to deliver real-time astronomy information based on user location.

---

## Core Development Principles

### 1. Simplicity First
- **Prioritize simple solutions** over complex architectures
- Avoid over-engineering; implement the most straightforward approach that meets requirements
- If a solution requires extensive explanation, consider if there's a simpler alternative
- Use built-in .NET and Blazor features before introducing third-party libraries
- Keep component hierarchies shallow and logical

### 2. Code Quality Standards

#### Reusability
- Extract common functionality into shared services or components
- Create utility methods for repeated logic
- Use parameterized components for variations of similar UI elements
- Design services with single responsibility in mind
- Make components self-contained and portable

#### Scalability
- Structure code to accommodate future features without major refactoring
- Use dependency injection for loose coupling
- Design APIs and services with extensibility in mind
- Consider pagination and lazy loading for data-heavy features
- Plan for asynchronous operations from the start

#### Maintainability
- Write self-documenting code with clear naming conventions
- Add comments for complex logic or business rules
- Keep methods focused and concise (ideally under 50 lines)
- Follow consistent patterns throughout the codebase
- Document API integrations and external dependencies

### 3. DRY (Don't Repeat Yourself)
- **Never duplicate code** - extract to methods, services, or components
- Create base classes or interfaces for shared behavior
- Use constants for magic strings and numbers
- Implement shared CSS classes for common styling patterns
- Centralize configuration and API endpoints
- Create reusable Razor components for repeated markup

**Examples of DRY violations to avoid:**
```csharp
// ❌ Bad: Repeated API call logic
var response1 = await httpClient.GetAsync(url1);
if (response1.IsSuccessStatusCode) { /* parse */ }

var response2 = await httpClient.GetAsync(url2);
if (response2.IsSuccessStatusCode) { /* parse */ }

// ✅ Good: Extract to reusable method
private async Task<T?> FetchApiDataAsync<T>(string url) { /* ... */ }
```

### 4. Project Coding Style

#### Naming Conventions
- **PascalCase**: Classes, methods, properties, public fields
- **camelCase**: Private fields (with underscore prefix for injected services), local variables, parameters
- **PascalCase**: Razor component filenames and component classes
- Descriptive names that reveal intent (e.g., `FetchWebbDataAsync` not `GetData`)

#### File Organization
```
Pages/           - Razor pages (routable components)
Layout/          - Layout components (MainLayout, NavMenu)
wwwroot/         - Static assets (CSS, JS, images)
  css/           - Stylesheets
  lib/           - Third-party libraries
Properties/      - Launch settings
docs/            - Documentation
```

#### Code Patterns
- Use `@code` blocks at the bottom of Razor files
- Place using directives in `_Imports.razor` when used across multiple files
- Prefer async/await for all I/O operations
- Use null-conditional operators (`?.`) and null-coalescing (`??`) appropriately
- Implement proper error boundaries in components

#### Comment Style
```csharp
// Single-line comments for brief explanations
// Use clear, complete sentences

/// <summary>
/// XML documentation for public APIs
/// Describes what the method does and its purpose
/// </summary>
/// <param name="parameter">Description of parameter</param>
/// <returns>Description of return value</returns>
```

### 5. Security Best Practices

#### API Key Management
- **Never hardcode API keys** in source code
- Use `appsettings.json` or environment variables
- Exclude sensitive configuration from version control
- Consider Azure Key Vault or similar for production secrets

#### Input Validation
- Validate and sanitize all user inputs
- Use parameterized queries (when applicable)
- Implement proper error handling without exposing sensitive details
- Validate data types and ranges

#### XSS Prevention
- Blazor automatically HTML-encodes by default - preserve this behavior
- Be cautious with `MarkupString` - only use with trusted content
- Sanitize any user-generated content before display

#### CORS and API Calls
- Configure CORS policies appropriately
- Use HTTPS for all external API calls
- Implement proper authentication and authorization where needed

### 6. Performance Optimization

#### Loading and Rendering
- Use `@key` directives for list rendering optimization
- Implement virtualization for long lists
- Lazy load components with `<LazyComponent>`
- Minimize initial bundle size - consider code splitting

#### Starry Background
- Use CSS animations over JavaScript where possible
- Optimize canvas operations (if using canvas)
- Consider reducing particle count on mobile devices
- Use `requestAnimationFrame` for smooth animations

#### API Calls
- Cache API responses when appropriate
- Implement proper loading states
- Use cancellation tokens for long-running operations
- Debounce user input that triggers API calls
- Consider implementing retry logic with exponential backoff

#### Memory Management
- Dispose of resources (`IDisposable`) properly
- Unsubscribe from events and clean up event handlers
- Use `IJSRuntime` efficiently
- Monitor component lifecycle and avoid memory leaks

---

## UI/UX Design Philosophy

### Surprise and Delight
**Generic, cookie-cutter interfaces are discouraged.** This astronomy app should feel magical and unique:

#### Visual Design
- **Starry background**: Twinkling, animated stars that create depth and wonder
- **Smooth animations**: Transitions should be fluid and purposeful
- **Color palette**: Deep space blues, purples, whites - evocative of night sky
- **Glassmorphism effects**: Translucent panels with backdrop blur for modern aesthetic
- **Subtle interactions**: Hover effects, micro-animations on user actions

#### User Experience
- **Progressive disclosure**: Don't overwhelm - reveal complexity gradually
- **Contextual help**: Tooltips and subtle hints without being intrusive
- **Delightful feedback**: Success states that feel rewarding
- **Smooth loading states**: Skeleton screens or elegant spinners, never jarring blank states
- **Error states**: Friendly, helpful messages with clear next steps

#### Accessibility (Never Compromise)
- Maintain WCAG 2.1 AA standards minimum
- Ensure sufficient color contrast (especially against starry backgrounds)
- Provide keyboard navigation for all interactive elements
- Include ARIA labels for screen readers
- Test with assistive technologies
- Support reduced motion preferences

#### Responsive Design
- Mobile-first approach
- Graceful degradation of visual effects on lower-end devices
- Touch-friendly targets (minimum 44x44px)
- Optimize for various screen sizes and orientations

#### Example Design Patterns
```css
/* Glassmorphism card */
.celestial-card {
    background: rgba(255, 255, 255, 0.1);
    backdrop-filter: blur(10px);
    border: 1px solid rgba(255, 255, 255, 0.2);
    border-radius: 16px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.37);
}

/* Smooth, purposeful animation */
.fade-in-up {
    animation: fadeInUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes fadeInUp {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
```

---

## Testing Requirements

### Unit Tests
- Write unit tests for critical components and services
- Use xUnit with bUnit for Blazor component testing
- Mock external dependencies (HttpClient, JSRuntime)
- Aim for meaningful test coverage, not arbitrary percentages
- Test edge cases and error conditions

### Test Organization
```
AboveMe.Tests/
  Services/        - Service tests
  Components/      - Component tests
  Integration/     - Integration tests
```

### Key Testing Principles
- **Arrange-Act-Assert** pattern
- One logical assertion per test
- Descriptive test names that explain what's being tested
- Mock external dependencies to isolate units under test

---

## Error Handling

### User-Facing Errors
- Display friendly, actionable error messages
- Provide context about what went wrong and how to fix it
- Never show stack traces or technical details to users
- Log detailed errors for debugging (consider Application Insights)

### API Failures
```csharp
try
{
    var response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    // Process response
}
catch (HttpRequestException ex)
{
    // Log detailed error
    logger.LogError(ex, "Failed to fetch data from {Url}", url);
    
    // Display user-friendly message
    errorMessage = "Unable to fetch celestial data. Please try again later.";
}
```

---

## Git and Version Control

### Commit Messages
- Use clear, descriptive commit messages
- Format: `[Type] Brief description`
- Types: Feature, Fix, Refactor, Docs, Style, Test, Chore

### Branch Strategy
- `main`: Production-ready code
- `develop-main`: Development branch
- Feature branches: `feature/feature-name`
- Bug fixes: `fix/bug-description`

---

## Documentation

### Code Comments
- Explain **why**, not **what** (code shows what)
- Document business rules and constraints
- Note any workarounds or technical debt
- Keep comments up-to-date with code changes

### README and Docs
- Keep README.md current with setup instructions
- Document new features in `/docs` folder
- Include API integration guides
- Provide examples for complex features

---

## API Integration Guidelines

### Current APIs
- **ipgeolocation**: User location detection
- **NASA Images API**: Webb Space Telescope images

### Best Practices
- Use `HttpClient` via dependency injection
- Implement retry logic for transient failures
- Cache responses when appropriate (with expiration)
- Handle rate limiting gracefully
- Document API requirements and limitations

---

## Continuous Improvement

### Code Reviews
- Review for adherence to these principles
- Check for security vulnerabilities
- Ensure tests are included
- Validate UI/UX meets quality standards

### Refactoring
- Regularly refactor to maintain code quality
- Address technical debt proactively
- Improve performance based on metrics
- Update dependencies and security patches

---

## Quick Reference Checklist

Before committing code, verify:
- [ ] Solution is as simple as possible
- [ ] No code duplication (DRY)
- [ ] Follows project naming conventions
- [ ] Includes appropriate error handling
- [ ] Has unit tests for critical paths
- [ ] UI is accessible and responsive
- [ ] Security best practices followed
- [ ] Performance optimized
- [ ] Documentation updated if needed
- [ ] Comments explain complex logic
- [ ] Design delights users (not generic)

---

## Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [bUnit Testing](https://bunit.dev/)
- [WCAG Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

---

*Last Updated: December 27, 2025*
