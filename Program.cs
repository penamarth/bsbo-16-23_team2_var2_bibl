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
            Console.WriteLine(
                $"[ReservationQueue] Внимание! ID книги ({bookId}) не совпадает с ID очереди ({_bookId}). Обновляем ID очереди.");
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
            Console.WriteLine(
                $"УВЕДОМЛЕНИЕ ОТПРАВЛЕНО -> Пользователю {nextReservation.AccountId} для книги {_bookId}");
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
        Console.WriteLine(
            $"Adding reservation with ID: {reservationId}. Current reservations: {CurrentReservations.Count}");
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
            reservationQueues[bookId] = new ReservationQueue(bookId);
        }

        var reservationId = reservationQueues[bookId].AddReservation(accountId, bookId);
        var reservation = new Reservation
        {
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


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ДЕМОНСТРАЦИЯ\n");

        var isb = new ISB();


        Console.WriteLine("ШАГ 1: Регистрация нового пользователя");
        Console.WriteLine("-----------------------------------");
        var account1 = isb.RegisterAccount("Анна Петрова", "+79161234567", "anna@mail.ru");
        var account2 = isb.RegisterAccount("Сергей Иванов", "+79039876543", "sergey@mail.ru");
        Console.WriteLine($"Зарегистрирован читатель 1: {account1.GetFullName()}, ID: {account1.GetId()}");
        Console.WriteLine($"Зарегистрирован читатель 2: {account2.GetFullName()}, ID: {account2.GetId()}\n");


        Console.WriteLine("ШАГ 2: Создание каталога и добавление книг");

        // Создаем книги
        var book1 = new Book
        {
            Id = "B001", Title = "Мастер и Маргарита", Author = "Михаил Булгаков", Year = 1967, Description = "Роман"
        };
        var book2 = new Book
        {
            Id = "B002", Title = "Преступление и наказание", Author = "Фёдор Достоевский", Year = 1866,
            Description = "Роман"
        };
        var book3 = new Book
            { Id = "B003", Title = "Война и мир", Author = "Лев Толстой", Year = 1869, Description = "Роман-эпопея" };

        // Создаем экземпляры книг
        var bookInstance1 = new BookInstance { Id = "BI001", BookId = "B001", Status = "Available" };
        var bookInstance2 = new BookInstance
            { Id = "BI002", BookId = "B001", Status = "Available" }; // Второй экземпляр
        var bookInstance3 = new BookInstance { Id = "BI003", BookId = "B002", Status = "Available" };
        var bookInstance4 = new BookInstance { Id = "BI004", BookId = "B003", Status = "Available" };

        // Создаем каталог и добавляем книги
        var catalog = new Catalog();
        catalog.AddBook(book1);
        catalog.AddBook(book2);
        catalog.AddBook(book3);

        // Размещаем книги в хранилище
        catalog.AddBookInstance(bookInstance1);
        catalog.AddBookInstance(bookInstance2);
        catalog.AddBookInstance(bookInstance3);
        catalog.AddBookInstance(bookInstance4);

        Console.WriteLine($"В каталог добавлено книг: {catalog.Books.Count}");
        Console.WriteLine($"Создано экземпляров: 4\n");
        
        Console.WriteLine("ШАГ 3: Поиск книг в каталоге");

        Console.WriteLine("Поиск по названию 'мастер':");
        var searchResults1 = catalog.SearchBooks("мастер", "");
        foreach (var book in searchResults1)
        {
            Console.WriteLine($"  Найдено: {book.Title} - {book.Author} ({book.Year})");
        }

        Console.WriteLine("\nПоиск по автору 'толстой':");
        var searchResults2 = catalog.SearchBooks("", "толстой");
        foreach (var book in searchResults2)
        {
            Console.WriteLine($"  Найдено: {book.Title} - {book.Author} ({book.Year})");
        }

        Console.WriteLine("\nПоиск всех книг:");
        var searchResults3 = catalog.SearchBooks("", "");
        Console.WriteLine($"  Всего найдено книг: {searchResults3.Count}\n");
        
        Console.WriteLine("ШАГ 4: Работа с ISB - сканирование QR-кода");


        string qrCodeData = account1.GetId(); // QR-код содержит ID пользователя
        string accountId = isb.ScanQRCode(qrCodeData);
        Console.WriteLine($"Сканирован QR-код: {qrCodeData}");
        Console.WriteLine($"Получен ID аккаунта: {accountId}");

        bool accountExists = isb.TryFindAccount(accountId);
        Console.WriteLine($"Аккаунт существует: {accountExists}");

        var userInfo = isb.GetUserInfo(accountId);
        if (userInfo != null)
        {
            Console.WriteLine($"Информация о пользователе: {userInfo.GetFullName()}, статус: {userInfo.GetStatus()}\n");
        }
        
        Console.WriteLine("ШАГ 5: Проверка возможности взятия книг");

        Console.WriteLine("Проверка читателя Анна Петрова:");
        Console.WriteLine($"  Есть просроченные книги: {account1.HasOverdueBooks()}");
        Console.WriteLine($"  Есть штрафы: {account1.HasUnpaidFines()}");
        Console.WriteLine($"  Может взять еще книги: {account1.CanBorrowMore()}\n");
        
        Console.WriteLine("ШАГ 6: Выдача книги (когда книга доступна)");


        try
        {
            Console.WriteLine("Попытка выдать книгу 'Мастер и Маргарита' (ID: B001):");
            var bookLocation = catalog.FindBook("B001");
            if (bookLocation != null && bookLocation.IsAvailable)
            {
                Console.WriteLine($"  Книга найдена в хранилище, местоположение: {bookLocation.Location}");

                // Выдаем книгу на 14 дней
                var loan = isb.IssueBook(account1.GetId(), "B001", DateTime.Now, DateTime.Now.AddDays(14));
                Console.WriteLine($"  Книга выдана успешно!");
                Console.WriteLine($"  ID займа: {loan.Id}");
                Console.WriteLine($"  Дата возврата: {loan.DueDate:dd.MM.yyyy}");

                // Проверяем статус аккаунта после выдачи
                Console.WriteLine($"  У пользователя теперь книг на руках: {account1.CurrentLoans}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Ошибка: {ex.Message}");
        }
        
        Console.WriteLine("\nШАГ 7: Бронирование книги (когда все экземпляры выданы)");

        try
        {
            Console.WriteLine("Второй читатель пытается взять ту же книгу 'Мастер и Маргарита':");

            // Сначала проверяем доступность
            var bookLocation2 = catalog.FindBook("B001");
            if (bookLocation2 == null || !bookLocation2.IsAvailable)
            {
                Console.WriteLine("  Все экземпляры книги выданы, предлагаем бронирование");

                // Бронируем книгу
                var reservation = isb.ReserveBook(account2.GetId(), "B001");
                Console.WriteLine($"  Книга забронирована!");
                Console.WriteLine($"  ID бронирования: {reservation.Id}");
                Console.WriteLine($"  Позиция в очереди: 1 (первый в очереди)");

                // Проверяем статус аккаунта после бронирования
                Console.WriteLine($"  У пользователя активных бронирований: {account2.CurrentReservations.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Ошибка: {ex.Message}");
        }
        
        Console.WriteLine("\nШАГ 8: Работа с очередью бронирований");


        // Создаем очередь бронирований
        var reservationQueue = new ReservationQueue("B001");

        // Добавляем несколько бронирований
        Console.WriteLine("Добавляем бронирования в очередь:");
        var resId1 = reservationQueue.AddReservation("ACC001", "B001");
        var resId2 = reservationQueue.AddReservation("ACC002", "B001");
        var resId3 = reservationQueue.AddReservation("ACC003", "B001");

        // Проверяем позиции в очереди
        Console.WriteLine($"\nПроверяем позиции в очереди:");
        Console.WriteLine($"  ACC001 позиция: {reservationQueue.GetPosition("ACC001")}");
        Console.WriteLine($"  ACC002 позиция: {reservationQueue.GetPosition("ACC002")}");
        Console.WriteLine($"  ACC003 позиция: {reservationQueue.GetPosition("ACC003")}");

        // Получаем следующую бронь
        var nextReservation = reservationQueue.GetNextReservation();
        if (nextReservation != null)
        {
            Console.WriteLine($"\nСледующая бронь в очереди: {nextReservation.AccountId}");
            reservationQueue.NotifyNextReader();
        }
        
        Console.WriteLine("\nШАГ 9: Возврат книги");


        Console.WriteLine("Проверяем, есть ли долг по возвращенной книге:");
        bool hasDebt = isb.CheckReturnBook("B001");
        Console.WriteLine($"  Есть долг: {hasDebt}");

        Console.WriteLine("\nОбновляем информацию о возврате книги:");
        isb.UpdateReturnBook("B001");

        Console.WriteLine($"  У пользователя после возврата книг на руках: {account1.CurrentLoans}");
        
        Console.WriteLine("\nШАГ 10: Создание займа напрямую");


        var directLoan = new Loan("ACC999", "BI005", DateTime.Now, DateTime.Now.AddDays(21));
        Console.WriteLine($"Создан займ напрямую:");
        Console.WriteLine($"  ID: {directLoan.Id}");
        Console.WriteLine($"  Экземпляр книги: {directLoan.BookItemId}");
        Console.WriteLine($"  Срок возврата: {directLoan.DueDate:dd.MM.yyyy}");
        
        Console.WriteLine("\nШАГ 11: Проверка композитной структуры");


        Console.WriteLine("Проверяем доступные места в хранилище:");
        var availableLocation = catalog.FindAvailableLocation();
        if (availableLocation != null)
        {
            Console.WriteLine($"  Найдено свободное место в хранилище");
        }
        else
        {
            Console.WriteLine($"  Свободных мест нет");
        }
    }
}
