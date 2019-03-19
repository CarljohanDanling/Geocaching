﻿// <auto-generated />
using System;
using Geocaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Geocaching.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.3-servicing-35854")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Geocaching.FoundGeocache", b =>
                {
                    b.Property<int>("PersonID");

                    b.Property<int>("GeocacheID");

                    b.HasKey("PersonID", "GeocacheID");

                    b.HasIndex("GeocacheID");

                    b.ToTable("FoundGeocache");
                });

            modelBuilder.Entity("Geocaching.Geocache", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Contents")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<int?>("PersonID");

                    b.HasKey("ID");

                    b.HasIndex("PersonID");

                    b.ToTable("Geocache");
                });

            modelBuilder.Entity("Geocaching.Person", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(50);

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(50);

                    b.HasKey("ID");

                    b.ToTable("Person");
                });

            modelBuilder.Entity("Geocaching.FoundGeocache", b =>
                {
                    b.HasOne("Geocaching.Geocache", "Geocache")
                        .WithMany("FoundGeocache")
                        .HasForeignKey("GeocacheID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Geocaching.Person", "Person")
                        .WithMany("FoundGeocache")
                        .HasForeignKey("PersonID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Geocaching.Geocache", b =>
                {
                    b.HasOne("Geocaching.Person", "Person")
                        .WithMany()
                        .HasForeignKey("PersonID");

                    b.OwnsOne("Geocaching.Coordinate", "GeoCoordinate", b1 =>
                        {
                            b1.Property<int>("GeocacheID")
                                .ValueGeneratedOnAdd()
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<double>("Latitude")
                                .HasColumnName("Latitude");

                            b1.Property<double>("Longitude")
                                .HasColumnName("Longitude");

                            b1.HasKey("GeocacheID");

                            b1.ToTable("Geocache");

                            b1.HasOne("Geocaching.Geocache")
                                .WithOne("GeoCoordinate")
                                .HasForeignKey("Geocaching.Coordinate", "GeocacheID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });

            modelBuilder.Entity("Geocaching.Person", b =>
                {
                    b.OwnsOne("Geocaching.Address", "Address", b1 =>
                        {
                            b1.Property<int>("PersonID")
                                .ValueGeneratedOnAdd()
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<string>("City")
                                .IsRequired()
                                .HasColumnName("City")
                                .HasMaxLength(50);

                            b1.Property<string>("Country")
                                .IsRequired()
                                .HasColumnName("Country")
                                .HasMaxLength(50);

                            b1.Property<string>("StreetName")
                                .IsRequired()
                                .HasColumnName("StreetName")
                                .HasMaxLength(50);

                            b1.Property<byte>("StreetNumber")
                                .HasColumnName("StreetNumber");

                            b1.HasKey("PersonID");

                            b1.ToTable("Person");

                            b1.HasOne("Geocaching.Person")
                                .WithOne("Address")
                                .HasForeignKey("Geocaching.Address", "PersonID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });

                    b.OwnsOne("Geocaching.Coordinate", "GeoCoordinate", b1 =>
                        {
                            b1.Property<int>("PersonID")
                                .ValueGeneratedOnAdd()
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<double>("Latitude")
                                .HasColumnName("Latitude");

                            b1.Property<double>("Longitude")
                                .HasColumnName("Longitude");

                            b1.HasKey("PersonID");

                            b1.ToTable("Person");

                            b1.HasOne("Geocaching.Person")
                                .WithOne("GeoCoordinate")
                                .HasForeignKey("Geocaching.Coordinate", "PersonID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
