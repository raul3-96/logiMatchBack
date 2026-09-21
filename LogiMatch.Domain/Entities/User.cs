using System.Net.Mail;
using LogiMatch.Domain;

namespace LogiMatch.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string Phone { get; private set; } = null!;

    public string? PasswordHash { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public UserStatus Status { get; private set; }

    private User()
    {
    }

    public User(
        string email,
        string firstName,
        string lastName,
        string phone)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException(
                "Email cannot be empty.");

        try
        {
            var mailAddress = new MailAddress(email);

            if (mailAddress.Address != email.Trim())
                throw new InvalidOperationException(
                    "Email format is invalid.");
        }
        catch
        {
            throw new InvalidOperationException(
                "Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
            throw new InvalidOperationException(
                "First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new InvalidOperationException(
                "Last name cannot be empty.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new InvalidOperationException(
                "Phone cannot be empty.");

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        CreatedAt = DateTime.UtcNow;
        Status = UserStatus.Active;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new InvalidOperationException(
                "Password hash cannot be empty.");

        PasswordHash = passwordHash;
    }
}