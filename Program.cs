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
    bool PlaceBook(BookInstance book);
}

// Базовый класс для информации о книге
public class BookInfo
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public int Year { get; set; }
    public string Description { get; set; }
}

// Базовый класс для расположения книги
public class BookLocation
{
    public string BookId { get; set; }
    public IComponent Location { get; set; }
    public bool IsAvailable { get; set; }
    public BookInstance BookInstance { get; set; }
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
    public Book Book { get; set; }
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
    public string Id { get; set; }

    public Cell(string id)
    {
        Id = id;
    }

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
        if (BookItem != null && BookItem.Book != null)
        {
            // Проверяем соответствие поисковому запросу
            bool matches = true;
            if (!string.IsNullOrEmpty(title) && 
                !BookItem.Book.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
                matches = false;
            if (!string.IsNullOrEmpty(author) && 
                !BookItem.Book.Author.Contains(author, StringComparison.OrdinalIgnoreCase))
                matches = false;
            
            if (matches)
            {
                result.Add(new BookInfo 
                { 
                    Id = BookItem.Book.Id,
                    Title = BookItem.Book.Title,
                    Author = BookItem.Book.Author,
                    Year = BookItem.Book.Year,
                    Description = BookItem.Book.Description
                });
            }
        }
        return result;
    }
}


// Класс Shelf
public class Shelf : IComponent
{
    public string Id { get; set; }
    public List<Cell> Children { get; set; } = new List<Cell>();

    public Shelf(string id, int cellCount)
    {
        Id = id;
        for (int i = 0; i < cellCount; i++)
        {
            Children.Add(new Cell($"{id}-Cell-{i}"));
        }
    }

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
    public string Id { get; set; }
    public List<Shelf> Children { get; set; } = new List<Shelf>();

    public Cabinet(string id, int shelfCount, int cellsPerShelf)
    {
        Id = id;
        for (int i = 0; i < shelfCount; i++)
        {
            Children.Add(new Shelf($"{id}-Shelf-{i}", cellsPerShelf));
        }
    }

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
public class Catalog : IComponent
{
    public List<Book> Books { get; set; } = new List<Book>();
    private List<BookInstance> BookInstances { get; set; } = new List<BookInstance>();
    public Cabinet RootCabinet { get; set; } // Изменено с IComponent на Cabinet

    public Catalog()
    {
        // Инициализация хранилища
        InitializeStorage();
    }

    private void InitializeStorage()
    {
        // Создаем кабинет с 5 полками по 10 ячеек каждая
        RootCabinet = new Cabinet("MainCabinet", 5, 10);
    }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        if (RootCabinet != null)
        {
            return RootCabinet.SearchBooks(title, author);
        }

        // Альтернативный поиск по книгам
        return Books.Where(b =>
            (string.IsNullOrEmpty(title) || b.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(author) || b.Author.Contains(author, StringComparison.OrdinalIgnoreCase)))
            .Select(b => new BookInfo
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Year = b.Year,
                Description = b.Description
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
                IsAvailable = true,
                BookInstance = bookInstance
            };
        }
        return null;
    }

    public BookLocation FindBookByItemId(string bookItemId)
    {
        var bookInstance = BookInstances.FirstOrDefault(bi => bi.Id == bookItemId);
        if (bookInstance != null)
        {
            return new BookLocation
            {
                BookId = bookInstance.BookId,
                Location = bookInstance.Location,
                IsAvailable = bookInstance.IsAvailable(),
                BookInstance = bookInstance
            };
        }
        return null;
    }

    public IComponent FindAvailableLocation()
    {
        return RootCabinet?.FindAvailableLocation();
    }

    public IComponent GetBookLocation(string bookItemId)
    {
        var bookInstance = BookInstances.FirstOrDefault(bi => bi.Id == bookItemId);
        return bookInstance?.Location;
    }

    public void AddBook(Book book)
    {
        Books.Add(book);
    }

    public void AddBookInstance(BookInstance bookInstance)
    {
        // Находим соответствующую книгу
        var book = Books.FirstOrDefault(b => b.Id == bookInstance.BookId);
        if (book != null)
        {
            bookInstance.Book = book;
            book.BooksAvailable.Add(bookInstance.Id);
        }
        
        // Размещаем книгу в хранилище
        if (RootCabinet != null && RootCabinet.PlaceBook(bookInstance))
        {
            BookInstances.Add(bookInstance);
            Console.WriteLine($"Книга {bookInstance.BookId} размещена в хранилище");
        }
        else
        {
            Console.WriteLine($"Не удалось разместить книгу {bookInstance.BookId} - нет свободных мест");
        }
    }

    public bool PlaceBook(BookInstance book)
    {
        return RootCabinet?.PlaceBook(book) ?? false;
    }
}


// Класс Reservation
public class Reservation
{
    // Поля диаграммы (сделаны свойствами с private set для имитации приватных полей с доступом на чтение)
    public string Id { get; private set; }
    public string AccountId { get; private set; }
    public string BookId { get; private set; }
    public DateTime CreateDate { get; private set; }
    public string Status { get; private set; }
    public DateTime ExpiryDate { get; private set; }

    // Конструктор по умолчанию (необходим, так как AddReservation инициализирует объект)
    public Reservation() 
    {
        // Инициализируем ID сразу, или можно внутри AddReservation,
        // но обычно объект должен иметь ID.
        Id = Guid.NewGuid().ToString(); 
    }

    // Метод AddReservation согласно диаграмме: + addReservation(accountId: String, bookId: String): String
    public string AddReservation(string accountId, string bookId)
    {
        Console.WriteLine($"[Reservation] Вызван AddReservation. AccountId: {accountId}, BookId: {bookId}");

        AccountId = accountId;
        BookId = bookId;
        CreateDate = DateTime.Now;
        Status = "Active";
        ExpiryDate = DateTime.Now.AddDays(7); // Логика срока действия (например, 7 дней)

        Console.WriteLine($"[Reservation] Состояние обновлено: Id={Id}, Status={Status}, CreateDate={CreateDate}");
        return Id;
    }

    // Метод Cancel согласно диаграмме: + cancel(): void
    public void Cancel()
    {
        Console.WriteLine($"[Reservation] Вызван Cancel для брони {Id}. Текущий статус: {Status}");
        Status = "Cancelled";
        Console.WriteLine($"[Reservation] Новый статус: {Status}");
    }

    // Метод Fulfill согласно диаграмме: + fulfill(): void
    public void Fulfill()
    {
        Console.WriteLine($"[Reservation] Вызван Fulfill для брони {Id}. Текущий статус: {Status}");
        Status = "Fulfilled";
        Console.WriteLine($"[Reservation] Новый статус: {Status}");
    }

    public override string ToString()
    {
        return $"[Reservation: {Id}, Account: {AccountId}, Status: {Status}]";
    }
}

// Класс ReservationQueue
public class ReservationQueue
{
    // Поля диаграммы: - bookId: String, - reservations: List<Reservation>
    private string _bookId;
    private List<Reservation> _reservations = new List<Reservation>();

    // Конструктор не указан в диаграмме явно, но нужен для установки bookId
    public ReservationQueue(string bookId)
    {
        _bookId = bookId;
    }

    // Метод: + addReservation(accountId: String, bookId: String): String
    public string AddReservation(string accountId, string bookId)
    {
        Console.WriteLine($"\n[ReservationQueue] Добавление в очередь. Книга: {bookId}, Аккаунт: {accountId}");
        
        // В диаграмме этот метод принимает bookId, хотя сама очередь уже привязана к книге.
        // Возможно, это для сверки или если очередь общая (хотя диаграмма ISB говорит Map<String, ReservationQueue>).
        if (bookId != _bookId)
        {
            Console.WriteLine($"[ReservationQueue] Внимание! ID книги ({bookId}) не совпадает с ID очереди ({_bookId}). Обновляем ID очереди.");
            _bookId = bookId;
        }

        var reservation = new Reservation();
        string resId = reservation.AddReservation(accountId, bookId);
        
        _reservations.Add(reservation);
        Console.WriteLine($"[ReservationQueue] Бронь добавлена. Всего в очереди: {_reservations.Count}");
        
        return resId;
    }

    // Метод: + getNextReservation(): Reservation
    public Reservation GetNextReservation()
    {
        Console.WriteLine("[ReservationQueue] Запрос следующей активной брони...");
        var next = _reservations.FirstOrDefault(r => r.Status == "Active");
        
        if (next != null)
            Console.WriteLine($"[ReservationQueue] Найдена бронь: {next.Id} для {next.AccountId}");
        else
            Console.WriteLine("[ReservationQueue] Активных броней нет.");
            
        return next;
    }

    // Метод: + getPosition(accountId: String): int
    public int GetPosition(string accountId)
    {
        Console.WriteLine($"[ReservationQueue] Вычисление позиции для {accountId}...");
        var activeReservations = _reservations.Where(r => r.Status == "Active").ToList();
        var reservation = activeReservations.FirstOrDefault(r => r.AccountId == accountId);
        
        int pos = reservation != null ? activeReservations.IndexOf(reservation) + 1 : -1;
        Console.WriteLine($"[ReservationQueue] Позиция: {pos}");
        return pos;
    }

    // Метод: + notifyNextReader(): void (Был пропущен в вашем коде)
    public void NotifyNextReader()
    {
        Console.WriteLine("[ReservationQueue] NotifyNextReader вызван.");
        var nextReservation = GetNextReservation();
        if (nextReservation != null)
        {
            // Здесь должна быть логика NotificationService, но согласно классу Queue, мы просто инициируем процесс
            Console.WriteLine($"*** УВЕДОМЛЕНИЕ ОТПРАВЛЕНО *** Пользователю {nextReservation.AccountId} для книги {_bookId}");
        }
        else
        {
            Console.WriteLine("[ReservationQueue] Некого уведомлять.");
        }
    }
}

// Класс Loan
public class Loan
{
    // Поля диаграммы: - id, - accountId, - bookItemId, - issueDate, - dueDate
    public string Id { get; private set; }
    public string AccountId { get; private set; }
    public string BookItemId { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }

    // Конструктор: + Loan(accountId: String, bookItemId: String, issueDate: Date, dueDate: Date)
    public Loan(string accountId, string bookItemId, DateTime issueDate, DateTime dueDate)
    {
        Console.WriteLine($"\n[Loan] Создание займа. Аккаунт: {accountId}, Экземпляр: {bookItemId}");
        
        Id = Guid.NewGuid().ToString();
        AccountId = accountId;
        BookItemId = bookItemId;
        IssueDate = issueDate;
        DueDate = dueDate;

        Console.WriteLine($"[Loan] Займ создан успешно. ID: {Id}, Срок возврата: {DueDate}");
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
        Console.WriteLine("Account: Constructor");
        Console.WriteLine($"Creating new account with data - fullName: {fullName}, phone: {phone}, email: {email}");
        Id = Guid.NewGuid().ToString();
        FullName = fullName;
        Phone = phone;
        Email = email;
        Status = "Active";
        CurrentLoans = 0;
        Console.WriteLine($"Initial values ​​are set, status is active");
        Console.WriteLine($"Account created with QR-code: {Id}");
    }

    public bool HasOverdueBooks()
    {
        // Проверка просроченных книг
        Console.WriteLine("Account: HasOverdueBooks");
        Console.WriteLine("Checking if account {Id} has overdue books");
        Console.WriteLine($"CurrentLoans (overdue count) = {CurrentLoans}");
        // Если CurrentLoans > 0, значит есть просроченные книги
        bool result = CurrentLoans > 0;
        return result;
    }

    public bool HasUnpaidFines()
    {
        Console.WriteLine("Account: HasUnpaidFines");
        Console.WriteLine("Checking if account has unpaid fines");
        // Проверка неуплаченных штрафов
        bool result = false; // Временная заглушка
        Console.WriteLine($"Result: {result}");
        return result;
    }

    public bool CanBorrowMore()
    {
        Console.WriteLine("Account: CanBorrowMore");
        Console.WriteLine($"Checking if account can borrow more books. Current loans: {CurrentLoans}, Max allowed: 5");
        bool hasOverdue = HasOverdueBooks();
        bool hasFines = HasUnpaidFines();
        bool isActive = Status == "Active";
        int maxBooksAllowed = 5;
        bool withinLimit = CurrentLoans < maxBooksAllowed;
        
        Console.WriteLine($"Parameters - BooksOnHand.Count: {BooksOnHand.Count}, " +
                         $"max allowed: {maxBooksAllowed}, withinLimit: {withinLimit}");
        Console.WriteLine($"hasOverdue: {hasOverdue}, hasFines: {hasFines}, isActive: {isActive}");
        
        bool result = withinLimit && !hasOverdue && !hasFines && isActive;
        
        if (!result)
        {
            Console.WriteLine($"Account {Id} cannot borrow more books. Reasons:");
            if (!withinLimit) Console.WriteLine($"  - Reached book limit: {BooksOnHand.Count}/{maxBooksAllowed}");
            if (hasOverdue) Console.WriteLine($"  - Has {CurrentLoans} overdue book(s)");
            if (hasFines) Console.WriteLine($"  - Has unpaid fines ({CurrentLoans} overdue books)");
            if (!isActive) Console.WriteLine($"  - Account is not active (status: {Status})");
        }
        
        return result;
    }

    public void AddLoan(string loanId)
    {
        Console.WriteLine("Account: AddLoan");
        Console.WriteLine($"Adding loan with ID: {loanId}. Current loans before: {CurrentLoans}");
        CurrentLoans++;
        Console.WriteLine($"Current loans after: {CurrentLoans}");
        // BooksOnHand будет обновляться при создании Loan
    }

    public void AddReservation(string reservationId)
    {
        Console.WriteLine("Account: AddReservation");
        Console.WriteLine($"Adding reservation with ID: {reservationId}. Current reservations: {CurrentReservations.Count}");
        CurrentReservations.Add(reservationId);
        Console.WriteLine($"Reservation added. New reservation count: {CurrentReservations.Count}");
    }

    public string GetFullName()
    {
        Console.WriteLine("Account: GetFullName");
        Console.WriteLine($"Returning full name: {FullName}");
        return FullName;
    }
    
    public string GetPhone()
    {
        Console.WriteLine("Account: GetPhone");
        Console.WriteLine($"Returning phone: {Phone}");
        return Phone;
    }
    
    public string GetEmail()
    {
        Console.WriteLine("Account: GetEmail");
        Console.WriteLine($"Returning email: {Email}");
        return Email;
    }
    
    public string GetId()
    {
        Console.WriteLine("Account: GetId");
        Console.WriteLine($"Returning ID: {Id}");
        return Id;
    }
    
    public string GetStatus()
    {
        Console.WriteLine("Account: GetStatus");
        Console.WriteLine($"Returning status: {Status}");
        return Status;
    }

    public bool CheckStatus(int id)
    {
        Console.WriteLine("Account: CheckStatus");
        Console.WriteLine($"Checking if account with ID {id} is active. Current status: {Status}");
        bool result = Status == "Active";
        Console.WriteLine($"Result: {result}");
        return result;
    }

    public void SetStatus(string status)
    {
        Console.WriteLine("Account: SetStatus");
        Console.WriteLine($"Changing status from '{Status}' to '{status}'");
        Status = status;
        Console.WriteLine($"Status updated. New status: {Status}");
    }

    public Loan CreateLoan(string accountId, string bookItemId, DateTime issueDate, DateTime dueDate)
    {
        Console.WriteLine("Account: CreateLoan");
        Console.WriteLine($"Creating loan - accountId: {accountId}, bookItemId: {bookItemId}, " +
                         $"issueDate: {issueDate}, dueDate: {dueDate}");
        var loan = new Loan(accountId, bookItemId, issueDate, dueDate);
        Console.WriteLine($"Loan created with ID: {loan.Id}");
        return loan;
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
        Console.WriteLine($"ISB: RegisterAccount: name - {fullName}, phone - {phone}, email - {email}");
        var account = new Account(fullName, phone, email);
        accounts[account.Id] = account;
        Console.WriteLine($"ISB: RegisterAccount: the Account object is added to the accounts collection");
        return account;
    }

    public Account Authenticate(string login, string password)
    {
        //пытаемся найти первый аккаунт в хранилище
        Console.WriteLine("ISB: Authenticate");
        Console.WriteLine("Try to find account");
        var result = accounts.Values.FirstOrDefault(a => a.Email == login);
        Console.WriteLine($"result of search: {result}");
        return result;
    }

    public List<BookInfo> SearchBooks(string title, string author)
    {
        Console.WriteLine($"ISB: Search book by pattern. Book parametrs: title - {title}, author - {author}");
        //поиск через паттерн
        return catalog.SearchBooks(title, author);
    }

    public Reservation ReserveBook(string accountId, string bookId)
    {
        Console.WriteLine($"ISB: Reserve book: account: {accountId}, book: {bookId}");
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
        Console.WriteLine("add reservation");
        accounts[accountId].AddReservation(reservationId);
        return reservation;
    }

    public Loan IssueBook(string accountId, string bookId, DateTime issueDate, DateTime dueDate)
    {
        Console.WriteLine($"ISB: Issue book: {bookId}, start date: {issueDate}, end date: {dueDate}");
        if (!accounts.ContainsKey(accountId) || !accounts[accountId].CanBorrowMore())
            throw new InvalidOperationException("Cannot issue book to this account. Dont have permissions");

        Console.WriteLine("try to find book in catalog");
        var bookLocation = catalog.FindBook(bookId);
        if (bookLocation == null || !bookLocation.IsAvailable)
            throw new InvalidOperationException("Book not available");

        Console.WriteLine("create loan");
        var loan = accounts[accountId].CreateLoan(accountId, bookId, issueDate, dueDate);
        activeLoans[loan.Id] = loan;
        accounts[accountId].AddLoan(loan.Id);
        accounts[accountId].BooksOnHand.Add(bookId);

        return loan;
    }

    public bool CheckReturnBook(string bookId)
    {
        Console.WriteLine("ISB: check return book");
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
        Console.WriteLine("ISB: update return book");
        //ищем задолженность 
        var loan = activeLoans.Values.FirstOrDefault(l => l.BookItemId == bookId);
        if (loan != null)
        {
            var account = accounts[loan.AccountId];
            Console.WriteLine("currentLoans - 1");
            account.CurrentLoans--; //уменьшаем задолженность
            Console.WriteLine("pick up the book (also in account info) :)");
            account.BooksOnHand.Remove(bookId); //забираем книгу из рук
            activeLoans.Remove(loan.Id);
        }
    }

    public Account GetUserInfo(string id)
    {
        Console.WriteLine("ISB: get user info if exists");
        //запись вида: если есть аккаунт - то вернём аккаунт, иначе null
        return accounts.ContainsKey(id) ? accounts[id] : null;
    }

    public string ScanQRCode(string qrData)
    {
        Console.WriteLine("ISB: scan qr code to get account id");
        // Парсинг QR-кода, по факту передаём id, но подразумеваем qr
        return qrData;
    }

    public bool TryFindAccount(string id)
    {
        Console.WriteLine("ISB: try find account");
        return accounts.ContainsKey(id);
    }

    public Dictionary<string, Loan> GetActiveLoans()
    {
        Console.WriteLine("ISB: get active loans");
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
