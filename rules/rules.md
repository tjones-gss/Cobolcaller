# Global Shop Solutions Development Rules

## General Guidelines

- Follow established coding standards for COBOL, VB.NET, and C#
- Include clear, concise comments for complex business logic
- Avoid deprecated functions or libraries
- All database access must go through the Data Layer module
- Use meaningful variable and program names that reflect business purpose

## COBOL-Specific Rules

### Program Structure
- Every program must have a clear PROGRAM-ID that matches the file name
- Include a header comment block with:
  - Program purpose
  - Author and date
  - Modification history
  - Input/output parameters

### Naming Conventions
- Program IDs: 6-8 characters (e.g., SCR100, UTL001)
- Working storage variables: Use descriptive prefixes (WS-, LS-)
- Paragraph names: Use verb-noun format (CALCULATE-TOTAL, VALIDATE-INPUT)

### Data Layer Access
- Never use direct SQL in business logic programs
- Always call designated data access programs (e.g., DBREAD, DBWRITE)
- Use standard copybooks for common data structures

### Error Handling
- Include DECLARATIVES for file error handling
- Use standard return codes (00 = success, 01-99 = specific errors)
- Log all critical errors to the error logging system

## VB.NET & C# Rules

### Code Organization
- One class per file (except for nested classes)
- Use namespaces that reflect the project structure
- Follow .NET naming conventions (PascalCase for public, camelCase for private)

### Error Handling
- Use try-catch blocks for expected exceptions
- Let unexpected exceptions bubble up to global handlers
- Log exceptions with full stack traces

### Database Access
- Use parameterized queries to prevent SQL injection
- Implement using statements for IDisposable objects
- Use connection pooling (ADO.NET default)

### Comments
- XML documentation comments for all public APIs
- Inline comments for complex algorithms only
- Let code be self-documenting through clear naming

## Version Control

- Commit frequently with descriptive messages
- Use feature branches for new development
- Include ticket/issue numbers in commit messages (e.g., "Fix #123: Correct tax calculation")
- Review code before merging to main branch

## Testing

- Test all changes in development environment before UAT
- Include unit tests for C# business logic
- Document test cases for COBOL programs
- Validate data integrity after database changes

## Documentation

- Update README files when adding new features
- Maintain call graphs for complex COBOL systems
- Document API changes immediately
- Keep Monday.com tickets updated with progress

## AI-Assisted Development (Cursor)

- Always provide context using CursorOps before asking for code changes
- Include relevant call graphs for COBOL modifications
- Review AI-generated code carefully before committing
- Test AI-generated changes thoroughly
- Document any AI-assisted refactoring decisions

## Security

- Never commit credentials or connection strings
- Use configuration files for environment-specific settings
- Sanitize all user inputs
- Follow principle of least privilege for database access
- Review security implications of legacy code changes

---

**Last Updated:** October 2024
**Maintained By:** GSS Development Team
