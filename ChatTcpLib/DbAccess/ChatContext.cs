using ChatTcpLib.Model;
using Microsoft.EntityFrameworkCore;

namespace ChatTcpLib.DbAccess;

public sealed class ChatContext : DbContext
{
    private readonly string _sqlServerName;
    public ChatContext(string sqlServerName)
    {
        _sqlServerName = sqlServerName;
        Database.EnsureCreated();
    }
    public DbSet<Message> Messages { get; private set; } = null!;
    public DbSet<User> Users { get; private set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer($"Server={_sqlServerName};Database=TextChatTCP_DB;" +
                                    $"Trusted_Connection=True;TrustServerCertificate=True");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>()
            .HasOne(u => u.Sender)
            .WithMany(m => m.MessageSender)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Message>()
            .HasOne(u => u.Recipient)
            .WithMany(m => m.MessageRecipient)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Message>()
            .ToTable(t => t.HasCheckConstraint("CK_Sender_Not_Recipient", "[SenderId] not like [RecipientId]"));
    }

    public User? GetUserByName(string name)
    {
        var result = Users.Where( user => user.Name == name );
        return result.Any() ? result.Single() : null;
    }

    public List<User> GetUsers(string name)
    {
        var result = Users.Where( user => user.Name != name );
        return result.ToList();
    }

    public List<Message> GetDialogueMessages(string senderName, string recipientName)
    {
        var result = Messages.Where( message =>
            message.Sender.Name == senderName && message.Recipient.Name == recipientName ||
            message.Sender.Name == recipientName && message.Recipient.Name == senderName );
        return result.ToList();
    }
}