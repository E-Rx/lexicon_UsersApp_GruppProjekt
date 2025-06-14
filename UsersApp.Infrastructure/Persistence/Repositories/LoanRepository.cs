﻿using Microsoft.EntityFrameworkCore;
using UsersApp.Application.Dtos;
using UsersApp.Application.Interfaces.Loans;
using UsersApp.Domain.Entities;
using UsersApp.Domain.Enums.Entities;

namespace UsersApp.Infrastructure.Persistence.Repositories;

public class LoanRepository(ApplicationContext context) : ILoanRepository
{
    public async Task AddAsync(Loan loan)
    {
        await context.AddAsync(loan);
        await context.Books!
            .Where(b => b.ISBN == loan.ISBN)
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.Status, BookStatus.Loaned));
        await context.SaveChangesAsync(); // TODO - Add UnitofWork    
    }

    public Loan[] GetAll() => [.. context.Loans!];

    public Loan? GetById(Guid id)
        => context.Loans!
        .SingleOrDefault(l => l.Id == id);


    public async Task RemoveAsync(Loan loan)
    {
        context.Remove(loan);
        await context.Books!
            .Where(b => b.ISBN == loan.ISBN)
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.Status, BookStatus.Available));
        await context.SaveChangesAsync();
    }

    public async Task<LoanDto[]> GetAllByUserIdAsync(Guid userId) =>
    await context.Users!
            .Where(u => u.Id == userId.ToString())
            .Join(
                context.Loans!,
                u => u.Id,
                l => l.UserId.ToString(),
                (user, loan) => new { user, loan }
            )
            .Join(
                context.Books!,
                lj => lj.loan.ISBN,
                b => b.ISBN,
                (lj, b) => new
                {
                    lj.loan.Id,
                    b.ISBN,
                    b.Title,
                    lj.loan.DueDate
                }
            )
            .OrderBy(l => l.DueDate)
            .Select(l => new LoanDto
            (
                l.Id,
                l.ISBN,
                l.Title,
                l.DueDate
            )).ToArrayAsync();
}

