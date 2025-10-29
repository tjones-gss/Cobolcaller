# Refactor Prompt

## Objective
Refactor the provided code for improved clarity, maintainability, and adherence to team standards.

## Requirements

### Code Quality
- Simplify complex logic without changing functionality
- Extract repeated code into reusable procedures/methods
- Improve variable and function naming for clarity
- Remove dead code and unused variables
- Add comments for non-obvious business logic

### COBOL-Specific
- Consolidate duplicate PERFORM statements
- Use structured programming (avoid GO TO when possible)
- Organize WORKING-STORAGE logically
- Extract complex calculations into separate paragraphs
- Use meaningful paragraph names

### .NET-Specific
- Apply SOLID principles where appropriate
- Use modern C# features (LINQ, null-coalescing, pattern matching)
- Extract magic numbers into named constants
- Consider async/await for I/O operations
- Reduce cyclomatic complexity

### Standards Compliance
- Follow team coding guidelines
- Maintain backward compatibility
- Preserve existing error handling
- Keep database access patterns consistent

## Deliverables
- Refactored code with clear diff from original
- Explanation of changes made and rationale
- List of any potential breaking changes or risks
- Suggestions for further improvements

## Constraints
- Do NOT change external interfaces or APIs
- Maintain existing business logic exactly
- Preserve all error handling behavior
- Keep performance characteristics similar or better
