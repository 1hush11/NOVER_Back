using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NOVER_Back.Models;

public partial class DbNoverContext : DbContext
{
    public DbNoverContext()
    {
    }

    public DbNoverContext(DbContextOptions<DbNoverContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Album> Albums { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Playlist> Playlists { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<Singer> Singers { get; set; }

    public virtual DbSet<Track> Tracks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPlaylist> UserPlaylists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Server=localhost;Database=db_nover;User Id=postgres;Password=1234");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("albums_pkey");

            entity.ToTable("albums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.SingerId).HasColumnName("singer_id");

            entity.HasOne(d => d.Singer).WithMany(p => p.Albums)
                .HasForeignKey(d => d.SingerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("albums_singer_id_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comments_pkey");

            entity.ToTable("comments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CommentText).HasColumnName("comment_text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Track).WithMany(p => p.Comments)
                .HasForeignKey(d => d.TrackId)
                .HasConstraintName("comments_track_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("comments_user_id_fkey");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("complaints_pkey");

            entity.ToTable("complaints");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Track).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.TrackId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("complaints_track_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("complaints_user_id_fkey");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("genres_pkey");

            entity.ToTable("genres");

            entity.HasIndex(e => e.Name, "genres_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("playlists_pkey");

            entity.ToTable("playlists");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");

            entity.HasOne(d => d.Creator).WithMany(p => p.Playlists)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("playlists_creator_id_fkey");

            entity.HasMany(d => d.Tracks).WithMany(p => p.Playlists)
                .UsingEntity<Dictionary<string, object>>(
                    "PlaylistTrack",
                    r => r.HasOne<Track>().WithMany()
                        .HasForeignKey("TrackId")
                        .HasConstraintName("playlist_tracks_track_id_fkey"),
                    l => l.HasOne<Playlist>().WithMany()
                        .HasForeignKey("PlaylistId")
                        .HasConstraintName("playlist_tracks_playlist_id_fkey"),
                    j =>
                    {
                        j.HasKey("PlaylistId", "TrackId").HasName("playlist_tracks_pkey");
                        j.ToTable("playlist_tracks");
                        j.IndexerProperty<int>("PlaylistId").HasColumnName("playlist_id");
                        j.IndexerProperty<int>("TrackId").HasColumnName("track_id");
                    });
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.TrackId }).HasName("ratings_pkey");

            entity.ToTable("ratings");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.Rating1).HasColumnName("rating");

            entity.HasOne(d => d.Track).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.TrackId)
                .HasConstraintName("ratings_track_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("ratings_user_id_fkey");
        });

        modelBuilder.Entity<Singer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("singers_pkey");

            entity.ToTable("singers");

            entity.HasIndex(e => e.Name, "singers_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Активен'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SubscribersCount)
                .HasDefaultValue(0)
                .HasColumnName("subscribers_count");
            entity.Property(e => e.ViewCount)
                .HasDefaultValue(0)
                .HasColumnName("view_count");

            entity.HasMany(d => d.Tracks).WithMany(p => p.Singers)
                .UsingEntity<Dictionary<string, object>>(
                    "SingerTrack",
                    r => r.HasOne<Track>().WithMany()
                        .HasForeignKey("TrackId")
                        .HasConstraintName("singer_tracks_track_id_fkey"),
                    l => l.HasOne<Singer>().WithMany()
                        .HasForeignKey("SingerId")
                        .HasConstraintName("singer_tracks_singer_id_fkey"),
                    j =>
                    {
                        j.HasKey("SingerId", "TrackId").HasName("singer_tracks_pkey");
                        j.ToTable("singer_tracks");
                        j.IndexerProperty<int>("SingerId").HasColumnName("singer_id");
                        j.IndexerProperty<int>("TrackId").HasColumnName("track_id");
                    });
        });

        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tracks_pkey");

            entity.ToTable("tracks");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AlbumId).HasColumnName("album_id");
            entity.Property(e => e.AudioUrl).HasColumnName("audio_url");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.GenreId).HasColumnName("genre_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PlayCount)
                .HasDefaultValue(0)
                .HasColumnName("play_count");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Активен'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Album).WithMany(p => p.Tracks)
                .HasForeignKey(d => d.AlbumId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tracks_album_id_fkey");

            entity.HasOne(d => d.Genre).WithMany(p => p.Tracks)
                .HasForeignKey(d => d.GenreId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tracks_genre_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Login, "users_login_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registration_date");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Активен'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasMany(d => d.Singers).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserSubscription",
                    r => r.HasOne<Singer>().WithMany()
                        .HasForeignKey("SingerId")
                        .HasConstraintName("user_subscriptions_singer_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("user_subscriptions_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "SingerId").HasName("user_subscriptions_pkey");
                        j.ToTable("user_subscriptions");
                        j.IndexerProperty<int>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("SingerId").HasColumnName("singer_id");
                    });

            entity.HasMany(d => d.Tracks).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "Library",
                    r => r.HasOne<Track>().WithMany()
                        .HasForeignKey("TrackId")
                        .HasConstraintName("libraries_track_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("libraries_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "TrackId").HasName("libraries_pkey");
                        j.ToTable("libraries");
                        j.IndexerProperty<int>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("TrackId").HasColumnName("track_id");
                    });
        });

        modelBuilder.Entity<UserPlaylist>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PlaylistId }).HasName("user_playlists_pkey");

            entity.ToTable("user_playlists");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PlaylistId).HasColumnName("playlist_id");
            entity.Property(e => e.IsOwner)
                .HasDefaultValue(false)
                .HasColumnName("is_owner");

            entity.HasOne(d => d.Playlist).WithMany(p => p.UserPlaylists)
                .HasForeignKey(d => d.PlaylistId)
                .HasConstraintName("user_playlists_playlist_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserPlaylists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_playlists_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
