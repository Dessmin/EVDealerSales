namespace EVDealerSales.Presentation.Helper
{
    public static class DbSeeder
    {
        //    public static async Task SeedUsersAsync(EVDealerSalesDbContext context)
        //    {
        //        // apply migrations if not yet applied
        //        await context.Database.MigrateAsync();

        //        if (!await context.Users.AnyAsync(u => u.Role == RoleType.DealerManager))
        //        {
        //            var passwordHasher = new PasswordHasher();
        //            var manager = new User
        //            {
        //                FullName = "Manager 1",
        //                Email = "manager@gmail.com",
        //                Phone = "0999000000",
        //                PasswordHash = passwordHasher.HashPassword("123")!,
        //                Role = RoleType.DealerManager,
        //                IsActive = true
        //            };
        //            await context.Users.AddAsync(manager);
        //        }

        //        if (!await context.Users.AnyAsync(u => u.Role == RoleType.DealerStaff))
        //        {
        //            var passwordHasher = new PasswordHasher();
        //            var staff = new User
        //            {
        //                FullName = "Staff 1",
        //                Email = "staff@gmail.com",
        //                Phone = "0888000000",
        //                PasswordHash = passwordHasher.HashPassword("123")!,
        //                Role = RoleType.DealerStaff,
        //                IsActive = true
        //            };
        //            await context.Users.AddAsync(staff);
        //        }

        //        await context.SaveChangesAsync();
        //    }

        //    public static async Task SeedVehiclesAsync(EVDealerSalesDbContext context)
        //    {
        //        if (!await context.Vehicles.AnyAsync())
        //        {
        //            var vehicles = new List<Vehicle>
        //            {
        //                new Vehicle
        //                {
        //                    ModelName = "Model S",
        //                    TrimName = "Plaid",
        //                    ModelYear = 2025,
        //                    BasePrice = 89990M,
        //                    ImageUrl = "https://images.unsplash.com/photo-1580273916550-e323be2ae537?q=80&w=764&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        //                    BatteryCapacity = 100M,
        //                    RangeKM = 637,
        //                    ChargingTime = 45, // minutes for 10-80%
        //                    TopSpeed = 322,
        //                    IsActive = true
        //                },
        //                new Vehicle
        //                {
        //                    ModelName = "iX",
        //                    TrimName = "M60",
        //                    ModelYear = 2025,
        //                    BasePrice = 108900M,
        //                    ImageUrl = "https://plus.unsplash.com/premium_photo-1664303847960-586318f59035?q=80&w=1074&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        //                    BatteryCapacity = 111.5M,
        //                    RangeKM = 561,
        //                    ChargingTime = 35,
        //                    TopSpeed = 250,
        //                    IsActive = true
        //                },
        //                new Vehicle
        //                {
        //                    ModelName = "EQS",
        //                    TrimName = "580 4MATIC",
        //                    ModelYear = 2025,
        //                    BasePrice = 125900M,
        //                    ImageUrl = "https://plus.unsplash.com/premium_photo-1683134240084-ba074973f75e?q=80&w=1595&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        //                    BatteryCapacity = 107.8M,
        //                    RangeKM = 587,
        //                    ChargingTime = 31,
        //                    TopSpeed = 210,
        //                    IsActive = true
        //                },
        //                new Vehicle
        //                {
        //                    ModelName = "Ioniq 6",
        //                    TrimName = "Limited AWD",
        //                    ModelYear = 2025,
        //                    BasePrice = 52600M,
        //                    ImageUrl = "https://images.unsplash.com/photo-1502877338535-766e1452684a?q=80&w=1172&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        //                    BatteryCapacity = 77.4M,
        //                    RangeKM = 509,
        //                    ChargingTime = 18,
        //                    TopSpeed = 230,
        //                    IsActive = true
        //                }
        //            };

        //            await context.Vehicles.AddRangeAsync(vehicles);
        //            await context.SaveChangesAsync();
        //        }
        //    }
        //    public static async Task SeedReportsDataAsync(EVDealerSalesDbContext context)
        //    {
        //        await context.Database.MigrateAsync();
        //        // 1. Customers
        //        if (!await context.Customers.AnyAsync())
        //        {
        //            await context.Customers.AddRangeAsync(new[]
        //            {
        //                new Customer { Id = Guid.NewGuid(), FirstName = "Alice", LastName=" Nguyen", Email = "alice@example.com", Address = "Q9", Phone ="0786315267" },
        //                new Customer { Id = Guid.NewGuid(), FirstName= "Bob ", LastName="Tran", Email = "bob@example.com", Address ="Q10", Phone ="0786315267" }
        //            });
        //            await context.SaveChangesAsync();
        //        }

        //        var staff = await context.Users.FirstAsync(u => u.Role == RoleType.DealerStaff);
        //        var customer1 = await context.Customers.FirstAsync();
        //        var customer2 = await context.Customers.Skip(1).FirstAsync();
        //        var vehicle1 = await context.Vehicles.FirstAsync(v => v.ModelName == "Model S");
        //        var vehicle2 = await context.Vehicles.FirstAsync(v => v.ModelName == "iX");

        //        // 2. Quotes
        //        if (!await context.Quotes.AnyAsync())
        //        {
        //            await context.Quotes.AddRangeAsync(new[]
        //            {
        //        new Quote
        //        {
        //            Id = Guid.NewGuid(),
        //            CustomerId = customer1.Id,
        //            StaffId = staff.Id,
        //            VehicleId = vehicle1.Id,
        //            QuotedPrice = 50000M,
        //            FinalPrice = 55000M,
        //            Status = QuoteStatus.Accepted,
        //            ValidUntil = DateTime.UtcNow.AddMonths(1),
        //            Remarks = "Good discount"
        //        },
        //        new Quote
        //        {
        //            Id = Guid.NewGuid(),
        //            CustomerId = customer2.Id,
        //            StaffId = staff.Id,
        //            VehicleId = vehicle2.Id,
        //            QuotedPrice = 80000M,
        //            FinalPrice = 88000M,
        //            Status = QuoteStatus.Accepted,
        //            ValidUntil = DateTime.UtcNow.AddMonths(1),
        //            Remarks = "Special offer"
        //        }
        //    });
        //            await context.SaveChangesAsync();
        //        }

        //        var quote1 = await context.Quotes.OrderBy(q => q.CreatedAt).FirstAsync();
        //        var quote2 = await context.Quotes.OrderByDescending(q => q.CreatedAt).FirstAsync();

        //        // 3. Orders
        //        if (!await context.Orders.AnyAsync())
        //        {
        //            await context.Orders.AddRangeAsync(new[]
        //            {
        //        new Order
        //        {
        //            Id = Guid.NewGuid(),
        //            QuoteId = quote1.Id,
        //            CustomerId = customer1.Id,
        //            StaffId = staff.Id,
        //            OrderDate = DateTime.UtcNow.AddMonths(-1),
        //            Status = OrderStatus.Confirmed,
        //            DiscountType = "Seasonal",
        //            DiscountValue = 5000M,
        //            DiscountNote = "Year-end promotion",
        //            SubtotalAmount = 50000M,
        //            TotalAmount = 55000M
        //        },
        //        new Order
        //        {
        //            Id = Guid.NewGuid(),
        //            QuoteId = quote2.Id,
        //            CustomerId = customer2.Id,
        //            StaffId = staff.Id,
        //            OrderDate = DateTime.UtcNow.AddMonths(-2),
        //            Status = OrderStatus.Confirmed,
        //            DiscountType = "Loyalty",
        //            DiscountValue = 8000M,
        //            DiscountNote = "Returning customer",
        //            SubtotalAmount = 80000M,
        //            TotalAmount = 88000M
        //        }
        //    });
        //            await context.SaveChangesAsync();
        //        }

        //        var order1 = await context.Orders.OrderBy(o => o.CreatedAt).FirstAsync();
        //        var order2 = await context.Orders.OrderByDescending(o => o.CreatedAt).FirstAsync();

        //        // 4. OrderItems
        //        if (!await context.OrderItems.AnyAsync())
        //        {
        //            await context.OrderItems.AddRangeAsync(new[]
        //            {
        //        new OrderItem
        //        {
        //            OrderId = order1.Id,
        //            VehicleId = vehicle1.Id,
        //            Quantity = 1,
        //            UnitPrice = 50000M,
        //            LineTotal = 50000M
        //        },
        //        new OrderItem
        //        {
        //            OrderId = order2.Id,
        //            VehicleId = vehicle2.Id,
        //            Quantity = 1,
        //            UnitPrice = 80000M,
        //            LineTotal = 80000M
        //        }
        //    });
        //            await context.SaveChangesAsync();
        //        }

        //        // 5. Invoices
        //        if (!await context.Invoices.AnyAsync())
        //        {
        //            await context.Invoices.AddRangeAsync(new[]
        //            {
        //        new Invoice
        //        {
        //            OrderId = order1.Id,
        //            CustomerId = order1.CustomerId,
        //            InvoiceNumber = "INV001",
        //            TotalAmount = order1.TotalAmount,
        //            Status = InvoiceStatus.Paid,
        //            DueDate = DateTime.UtcNow.AddMonths(-1).AddDays(7),
        //            Notes = "Invoice paid in full"
        //        },
        //        new Invoice
        //        {
        //            OrderId = order2.Id,
        //            CustomerId = order2.CustomerId,
        //            InvoiceNumber = "INV002",
        //            TotalAmount = order2.TotalAmount,
        //            Status = InvoiceStatus.Paid,
        //            DueDate = DateTime.UtcNow.AddMonths(-2).AddDays(7),
        //            Notes = "Invoice paid in full"
        //        }
        //    });
        //            await context.SaveChangesAsync();
        //        }

        //        var invoice1 = await context.Invoices.OrderBy(i => i.CreatedAt).FirstAsync();
        //        var invoice2 = await context.Invoices.OrderByDescending(i => i.CreatedAt).FirstAsync();

        //        // 6. Payments
        //        if (!await context.Payments.AnyAsync())
        //        {
        //            await context.Payments.AddRangeAsync(new[]
        //            {
        //        new Payment
        //        {
        //            InvoiceId = invoice1.Id,
        //            PaymentDate = DateTime.UtcNow.AddMonths(-1),
        //            Amount = invoice1.TotalAmount,
        //            Status = PaymentStatus.Paid
        //        },
        //        new Payment
        //        {
        //            InvoiceId = invoice2.Id,
        //            PaymentDate = DateTime.UtcNow.AddMonths(-2),
        //            Amount = invoice2.TotalAmount,
        //            Status = PaymentStatus.Paid
        //        }
        //    });
        //            await context.SaveChangesAsync();
        //        }

        //        // 7. TestDrives
        //        if (!await context.TestDrives.AnyAsync())
        //        {
        //            var testDrives = new TestDrive[]
        //            {
        //        new TestDrive
        //        {
        //            CustomerId = customer1.Id,
        //            VehicleId = vehicle1.Id,
        //            Status = TestDriveStatus.Completed,
        //            Notes = "Test drive for Model S",
        //            ScheduledAt = DateTime.UtcNow.AddDays(-10),
        //            StaffId = staff.Id
        //        },
        //        new TestDrive
        //        {
        //            CustomerId = customer2.Id,
        //            VehicleId = vehicle2.Id,
        //            Status = TestDriveStatus.Completed,
        //            Notes = "Test drive for iX",
        //            ScheduledAt = DateTime.UtcNow.AddDays(-5),
        //            StaffId = staff.Id
        //        }
        //            };

        //            await context.TestDrives.AddRangeAsync(testDrives); // <-- OK
        //            await context.SaveChangesAsync();
        //        }
        //}
    }
}
