using System.Net.Mail;

namespace LogiMatch.Domain.Entities;

public class Company
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string TaxId { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string Phone { get; private set; } = null!;

    public Guid OwnerUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Company()
    {
    }

    public Company(
        string name,
        string taxId,
        string email,
        string phone,
        Guid ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException(
                "Company name cannot be empty.");

        if (string.IsNullOrWhiteSpace(taxId))
            throw new InvalidOperationException(
                "Tax ID cannot be empty.");

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

        if (string.IsNullOrWhiteSpace(phone))
            throw new InvalidOperationException(
                "Phone cannot be empty.");

        if (ownerUserId == Guid.Empty)
            throw new InvalidOperationException(
                "Owner user ID cannot be empty.");

        Id = Guid.NewGuid();

        Name = name.Trim();
        TaxId = taxId.Trim().ToUpperInvariant();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        OwnerUserId = ownerUserId;
        CreatedAt = DateTime.UtcNow;
    }
}
