using System;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Base class for all domain-related exceptions
    /// </summary>
    public abstract class DomainException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this domain exception
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the DomainException class
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="message">The exception message</param>
        protected DomainException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        }

        /// <summary>
        /// Initializes a new instance of the DomainException class
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        protected DomainException(string errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        }
    }

    /// <summary>
    /// Exception thrown when a business rule is violated
    /// </summary>
    public class BusinessRuleViolationException : DomainException
    {
        /// <summary>
        /// Gets the name of the violated business rule
        /// </summary>
        public string RuleName { get; }

        /// <summary>
        /// Initializes a new instance of the BusinessRuleViolationException class
        /// </summary>
        /// <param name="ruleName">The name of the violated business rule</param>
        /// <param name="message">The exception message</param>
        public BusinessRuleViolationException(string ruleName, string message)
            : base($"BUSINESS_RULE_VIOLATION.{ruleName}", message)
        {
            RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
        }

        /// <summary>
        /// Initializes a new instance of the BusinessRuleViolationException class
        /// </summary>
        /// <param name="ruleName">The name of the violated business rule</param>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public BusinessRuleViolationException(string ruleName, string message, Exception innerException)
            : base($"BUSINESS_RULE_VIOLATION.{ruleName}", message, innerException)
        {
            RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
        }
    }

    /// <summary>
    /// Exception thrown when an entity is not found
    /// </summary>
    public class EntityNotFoundException : DomainException
    {
        /// <summary>
        /// Gets the type of the entity that was not found
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Gets the identifier that was not found
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Initializes a new instance of the EntityNotFoundException class
        /// </summary>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="id">The identifier that was not found</param>
        public EntityNotFoundException(string entityType, object id)
            : base("ENTITY_NOT_FOUND", $"{entityType} with id '{id}' was not found")
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>
        /// Initializes a new instance of the EntityNotFoundException class
        /// </summary>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="id">The identifier that was not found</param>
        /// <param name="innerException">The inner exception</param>
        public EntityNotFoundException(string entityType, object id, Exception innerException)
            : base("ENTITY_NOT_FOUND", $"{entityType} with id '{id}' was not found", innerException)
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }

    /// <summary>
    /// Exception thrown when a domain validation fails
    /// </summary>
    public class DomainValidationException : DomainException
    {
        /// <summary>
        /// Gets the name of the field or property that failed validation
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Initializes a new instance of the DomainValidationException class
        /// </summary>
        /// <param name="fieldName">The name of the field that failed validation</param>
        /// <param name="message">The validation error message</param>
        public DomainValidationException(string fieldName, string message)
            : base($"DOMAIN_VALIDATION.{fieldName}", message)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        }

        /// <summary>
        /// Initializes a new instance of the DomainValidationException class
        /// </summary>
        /// <param name="fieldName">The name of the field that failed validation</param>
        /// <param name="message">The validation error message</param>
        /// <param name="innerException">The inner exception</param>
        public DomainValidationException(string fieldName, string message, Exception innerException)
            : base($"DOMAIN_VALIDATION.{fieldName}", message, innerException)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        }
    }

    /// <summary>
    /// Exception thrown when a concurrency conflict occurs
    /// </summary>
    public class ConcurrencyException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the ConcurrencyException class
        /// </summary>
        /// <param name="message">The exception message</param>
        public ConcurrencyException(string message)
            : base("CONCURRENCY_CONFLICT", message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrencyException class
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public ConcurrencyException(string message, Exception innerException)
            : base("CONCURRENCY_CONFLICT", message, innerException)
        {
        }
    }
}