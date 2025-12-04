using System;
using System.Collections.Generic;
using System.Linq;

//Loan - займ книги (берём)
//Reservation - резервация книги (возьмём позже, как появится)

// Интерфейс Component
public interface IComponent
{
    IComponent FindAvailableLocation();
    List<BookInfo> SearchBooks(string title, string author);
}

// Базовый класс для информации о книге
public class BookInfo
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public int Year { get; set; }
}

// Базовый класс для расположения книги
public class BookLocation
{
    public string BookId { get; set; }
    public IComponent Location { get; set; }
    public bool IsAvailable { get; set; }
}

// Класс Book
public class Book
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public int Year { get; set; }
    public string Description { get; set; }
    public List<string> CurrentReservations { get; set; } = new List<string>();
    public List<string> BooksAvailable { get; set; } = new List<string>();
}

// Класс BookInstance
public class BookInstance
{
    public string Id { get; set; }
    public string BookId { get; set; }
    public string Status { get; set; }
    public IComponent Location { get; set; }
    public string UserId { get; set; }
    public DateTime? ReturnDate { get; set; }

    public void UpdateStatus(string status)
    {
        Status = status;
    }

    public bool IsAvailable()
    {
        return Status == "Available" && string.IsNullOrEmpty(UserId);
    }
}

// Класс Cell
public class Cell : IComponent
{
    public BookInstance BookItem { get; set; }

    public IComponent FindAvailableLocation()
    {
        return BookItem == null ? this : null;
    }

    public bool PlaceBook(BookInstance book)
    {
        if (BookItem == null)
        {
            BookItem = book;
            book.Location = this;
            return true;
        }
        return false;
    }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        var result = new List<BookInfo>();
        if (BookItem != null)
        {
            // Поиск по базе данных книг
            result.Add(new BookInfo { Id = BookItem.BookId });
        }
        return result;
    }
}

// Класс Shelf
public class Shelf : IComponent
{
    public List<Cell> Children { get; set; } = new List<Cell>();

    public IComponent FindAvailableLocation()
    {
        foreach (var cell in Children)
        {
            var availableLocation = cell.FindAvailableLocation();
            if (availableLocation != null)
                return availableLocation;
        }
        return null;
    }

    public bool PlaceBook(BookInstance book)
    {
        var availableLocation = FindAvailableLocation() as Cell;
        if (availableLocation != null)
        {
            return availableLocation.PlaceBook(book);
        }
        return false;
    }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        var result = new List<BookInfo>();
        foreach (var cell in Children)
        {
            result.AddRange(cell.SearchBooks(title, author));
        }
        return result;
    }
}

// Класс Cabinet
public class Cabinet : IComponent
{
    public List<Shelf> Children { get; set; } = new List<Shelf>();

    public IComponent FindAvailableLocation()
    {
        foreach (var shelf in Children)
        {
            var availableLocation = shelf.FindAvailableLocation();
            if (availableLocation != null)
                return availableLocation;
        }
        return null;
    }

    public bool PlaceBook(BookInstance book)
    {
        var availableLocation = FindAvailableLocation() as Cell;
        if (availableLocation != null)
        {
            return availableLocation.PlaceBook(book);
        }
        return false;
    }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        var result = new List<BookInfo>();
        foreach (var shelf in Children)
        {
            result.AddRange(shelf.SearchBooks(title, author));
        }
        return result;
    }
}

// Класс Catalog
public class Catalog
{
    public List<Book> Books { get; set; } = new List<Book>();
    private List<BookInstance> BookInstances { get; set; } = new List<BookInstance>();
    public IComponent RootComponent { get; set; }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        if (RootComponent != null)
        {
            return RootComponent.SearchBooks(title, author);
        }

        // Альтернативный поиск, если нет иерархии компонентов
        return Books.Where(b =>
            (string.IsNullOrEmpty(title) || b.Title.Contains(title)) &&
            (string.IsNullOrEmpty(author) || b.Author.Contains(author)))
            .Select(b => new BookInfo
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Year = b.Year
            }).ToList();
    }

    public BookLocation FindBook(string bookId)
    {
        var bookInstance = BookInstances.FirstOrDefault(bi => bi.BookId == bookId && bi.IsAvailable());
        if (bookInstance != null)
        {
            return new BookLocation
            {
                BookId = bookId,
                Location = bookInstance.Location,
                IsAvailable = true
            };
        }
        return null;
    }

    public IComponent FindAvailableLocation()
    {
        return RootComponent?.FindAvailableLocation();
    }

    public IComponent GetBookLocation(string bookItemId)
    {
        var bookInstance = BookInstances.FirstOrDefault(bi => bi.Id == bookItemId);
        return bookInstance?.Location;
    }

    public void AddBookInstance(BookInstance bookInstance)
    {
        BookInstances.Add(bookInstance);
    }
}

// Класс Reservation
public class Reservation
{
    public string Id { get; set; }
    public string AccountId { get; set; }
    public string BookId { get; set; }
    public DateTime CreateDate { get; set; }
    public string Status { get; set; }
    public DateTime ExpiryDate { get; set; }

    public string AddReservation(string accountId, string bookId)
    {
        AccountId = accountId;
        BookId = bookId;
        CreateDate = DateTime.Now;
        Status = "Active";
        ExpiryDate = DateTime.Now.AddDays(7);
        return Id;
    }

    public void Cancel()
    {
        Status = "Cancelled";
    }

    public void Fulfill()
    {
        Status = "Fulfilled";
    }
}

// Класс ReservationQueue
public class ReservationQueue
{
    public string BookId { get; set; }
    public List<Reservation> Reservations { get; set; } = new List<Reservation>();

    public string AddReservation(string accountId, string bookId)
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = accountId,
            BookId = bookId
        };
        reservation.AddReservation(accountId, bookId);
        Reservations.Add(reservation);
        return reservation.Id;
    }

    public Reservation GetNextReservation()
    {
        return Reservations.FirstOrDefault(r => r.Status == "Active");
    }

    public int GetPosition(string accountId)
    {
        var activeReservations = Reservations.Where(r => r.Status == "Active").ToList();
        var reservation = activeReservations.FirstOrDefault(r => r.AccountId == accountId);
        return reservation != null ? activeReservations.IndexOf(reservation) + 1 : -1;
    }

    // public void NotifyNextReader()
    // {
    //     var nextReservation = GetNextReservation();
    //     if (nextReservation != null)
    //     {
    //         // Отправка уведомления
    //         Console.WriteLine($"Notifying account {nextReservation.AccountId} about available book {BookId}");
    //     }
    // }
}

// Класс Loan
public class Loan
{
    public string Id { get; set; }
    public string AccountId { get; set; }
    public string BookItemId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }

    public Loan(string accountId, string bookItemId, DateTime issueDate, DateTime dueDate)
    {
        Id = Guid.NewGuid().ToString();
        AccountId = accountId;
        BookItemId = bookItemId;
        IssueDate = issueDate;
        DueDate = dueDate;
    }
}

// Класс Account
public class Account
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
    public int CurrentLoans { get; set; }
    public List<string> BooksOnHand { get; set; } = new List<string>();
    public List<string> CurrentReservations { get; set; } = new List<string>();

    public Account(string fullName, string phone, string email)
    {
        Id = Guid.NewGuid().ToString();
        FullName = fullName;
        Phone = phone;
        Email = email;
        Status = "Active";
        CurrentLoans = 0;
    }

    public bool HasOverdueBooks()
    {
        // Проверка просроченных книг
        return false;
    }

    public bool HasUnpaidFines()
    {
        // Проверка неуплаченных штрафов
        return false;
    }

    public bool CanBorrowMore()
    {
        return CurrentLoans < 5 && !HasOverdueBooks() && !HasUnpaidFines() && Status == "Active";
    }

    public void AddLoan(string loanId)
    {
        CurrentLoans++;
        // BooksOnHand будет обновляться при создании Loan
    }

    public void AddReservation(string reservationId)
    {
        CurrentReservations.Add(reservationId);
    }

    public string GetFullName() => FullName;
    public string GetPhone() => Phone;
    public string GetEmail() => Email;
    public string GetId() => Id;
    public string GetStatus() => Status;

    public bool CheckStatus(int id)
    {
        return Status == "Active";
    }

    public void SetStatus(string status)
    {
        Status = status;
    }

    public Loan CreateLoan(string accountId, string bookItemId, DateTime issueDate, DateTime dueDate)
    {
        return new Loan(accountId, bookItemId, issueDate, dueDate);
    }
}

// Сервис уведомлений (упрощенный)
public class Notification
{
    public void SendNotification(string accountId, string message)
    {
        Console.WriteLine($"Notification to {accountId}: {message}");
    }
}

// Главный класс ISB
public class ISB
{
    private Dictionary<string, Account> accounts = new Dictionary<string, Account>();
    private Dictionary<string, ReservationQueue> reservationQueues = new Dictionary<string, ReservationQueue>();
    private Dictionary<string, Loan> activeLoans = new Dictionary<string, Loan>();
    private Catalog catalog = new Catalog();
    private Notification notificationService = new Notification();

    public Account RegisterAccount(string fullName, string phone, string email)
    {
        var account = new Account(fullName, phone, email);
        accounts[account.Id] = account;
        return account;
    }

    public Account Authenticate(string login, string password)
    {
        //пытаемся найти первый аккаунт в хранилище
        return accounts.Values.FirstOrDefault(a => a.Email == login);
    }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        //поиск через паттерн
        return catalog.SearchBooks(title, author);
    }

    public Reservation ReserveBook(string accountId, string bookId)
    {
        if (!accounts.ContainsKey(accountId))
            throw new ArgumentException("Account not found");

        if (!reservationQueues.ContainsKey(bookId))
        {
            reservationQueues[bookId] = new ReservationQueue { BookId = bookId };
        }

        var reservationId = reservationQueues[bookId].AddReservation(accountId, bookId);
        var reservation = new Reservation
        {
            Id = reservationId,
            AccountId = accountId,
            BookId = bookId
        };

        accounts[accountId].AddReservation(reservationId);
        return reservation;
    }

    public Loan IssueBook(string accountId, string bookId, DateTime issueDate, DateTime dueDate)
    {
        if (!accounts.ContainsKey(accountId) || !accounts[accountId].CanBorrowMore())
            throw new InvalidOperationException("Cannot issue book to this account. Dont have permissions");

        var bookLocation = catalog.FindBook(bookId);
        if (bookLocation == null || !bookLocation.IsAvailable)
            throw new InvalidOperationException("Book not available");

        var loan = accounts[accountId].CreateLoan(accountId, bookId, issueDate, dueDate);
        activeLoans[loan.Id] = loan;
        accounts[accountId].AddLoan(loan.Id);
        accounts[accountId].BooksOnHand.Add(bookId);

        return loan;
    }

    public bool CheckReturnBook(string bookId)
    {
        // Проверка на долг по книге
        var loan = activeLoans.Values.FirstOrDefault(l => l.BookItemId == bookId);
        if (loan != null)
        {
            var daysOverdue = (DateTime.Now - loan.DueDate).Days;
            return daysOverdue > 0; // true если есть долг
        }
        return false;
    }

    public void UpdateReturnBook(string bookId)
    {
        //ищем задолженность 
        var loan = activeLoans.Values.FirstOrDefault(l => l.BookItemId == bookId);
        if (loan != null)
        {
            var account = accounts[loan.AccountId];
            account.CurrentLoans--; //уменьшаем задолженность
            account.BooksOnHand.Remove(bookId); //забираем книгу из рук
            activeLoans.Remove(loan.Id);
        }
    }

    public Account GetUserInfo(string id)
    {
        //запись вида: если есть аккаунт - то вернём аккаунт, иначе null
        return accounts.ContainsKey(id) ? accounts[id] : null;
    }

    public string ScanQRCode(string qrData)
    {
        // Парсинг QR-кода, по факту передаём id, но подразумеваем qr
        return qrData;
    }

    public bool TryFindAccount(string id)
    {
        return accounts.ContainsKey(id);
    }

    public Dictionary<string, Loan> GetActiveLoans()
    {
        //возвращаем копию, для безопасности
        return new Dictionary<string, Loan>(activeLoans);
    }
}

// Пример использования
class Program
{
    static void Main(string[] args)
    {
        var isb = new ISB();
        
        // Регистрация аккаунта
        var account = isb.RegisterAccount("Иван Иванов", "+79123456789", "ivan@example.com");
        
        // Поиск книг
        var books = isb.SearchBooks("C#", "Microsoft");
        
        // Резервация книги
        var reservation = isb.ReserveBook(account.Id, "book123");
        
        // Выдача книги
        var loan = isb.IssueBook(account.Id, "book123", DateTime.Now, DateTime.Now.AddDays(14));
        
        Console.WriteLine($"Account created: {account.FullName}");
        Console.WriteLine($"Books found: {books.Count}");
        Console.WriteLine($"Reservation ID: {reservation.Id}");
        Console.WriteLine($"Loan ID: {loan.Id}");
    }
}
