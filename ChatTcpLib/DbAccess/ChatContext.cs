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
    public DbSet<Message> Messages { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer($"Server={_sqlServerName};Database=TextChatTCP_DB;Trusted_Connection=True;TrustServerCertificate=True");
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
}