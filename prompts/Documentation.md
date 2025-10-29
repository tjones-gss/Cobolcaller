# Documentation Prompt

## Objective
Create comprehensive, maintainable documentation for the specified code or system.

## Documentation Scope

### Code-Level Documentation

#### COBOL Programs
- Program header with purpose, author, date, modification history
- WORKING-STORAGE section comments for complex data structures
- Paragraph/section comments explaining business logic
- Comments for non-obvious calculations or validations
- Data flow diagrams for complex processing

#### .NET Code
- XML documentation comments for all public APIs
- Summary, parameters, returns, exceptions for methods
- Class-level documentation of purpose and usage
- Inline comments for complex algorithms
- Examples for non-obvious usage patterns

### System-Level Documentation

#### Architecture
- System overview and component diagram
- Integration points with other systems
- Data flow diagrams
- Technology stack and dependencies

#### Call Graphs
- Entry points and main flows
- COBOL program call hierarchies
- Service/API dependencies
- Database access patterns

#### Business Logic
- Business rules and calculations
- Validation rules
- Error handling strategies
- Transaction boundaries

### User Documentation

#### Setup and Configuration
- Installation requirements
- Configuration file explanations
- Environment-specific settings
- Initial setup steps

#### Usage Examples
- Common use cases with examples
- Command-line arguments
- Expected inputs and outputs
- Error messages and resolutions

## Deliverables

### README.md
- Project overview
- Quick start guide
- Build and deployment instructions
- Common tasks and examples
- Troubleshooting section

### Inline Comments
- Updated code comments
- Complex logic explanations
- Business rule documentation
- TODO/FIXME items with context

### Architecture Docs
- Component diagrams
- Data flow diagrams
- Call graphs (use CursorOps for COBOL)
- Integration specifications

### Developer Guide
- Code organization
- Coding standards
- Testing approach
- Contribution guidelines

## Quality Criteria
- [ ] Documentation is accurate and up-to-date
- [ ] Examples are tested and working
- [ ] Language is clear and concise
- [ ] Audience-appropriate (dev vs. user docs)
- [ ] Diagrams are included where helpful
- [ ] Easy to find and navigate
