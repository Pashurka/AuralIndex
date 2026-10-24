using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuralIndex.Domain;
using AuralIndex.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuralIndex_App
{
    public partial class App : Microsoft.UI.Xaml.Application
    {
        private Microsoft.UI.Xaml.Window? _window;
        public static IHost HostContainer { get; private set; } = null!;
        public static MainWindow? Shell { get; private set; }

        public App()
        {
            this.InitializeComponent();

            HostContainer = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "AuralIndex",
                        "auralindex.db");

                    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                    services.AddInfrastructure($"Data Source={dbPath}");
                })
                .Build();

            using var scope = HostContainer.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuralIndexDbContext>();
            db.Database.EnsureCreated();
            EnsureUserEventsTable(db);
            EnsureFavoritesTable(db);
            EnsureIsListenedColumn(db);
            NormalizeLegacyLanguageCodes(db);
            EnsureGenreCoverage(db);
        }

        private static void EnsureUserEventsTable(AuralIndexDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS UserEvents (
                    Id INTEGER NOT NULL CONSTRAINT PK_UserEvents PRIMARY KEY AUTOINCREMENT,
                    EventType TEXT NOT NULL,
                    QueryText TEXT NULL,
                    BookId INTEGER NULL,
                    BookTitle TEXT NULL,
                    CreatedAtUtc TEXT NOT NULL
                );");

            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UserEvents_CreatedAtUtc ON UserEvents (CreatedAtUtc DESC);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UserEvents_BookId ON UserEvents (BookId);");
        }

        private static void EnsureFavoritesTable(AuralIndexDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS FavoriteBooks (
                    Id INTEGER NOT NULL CONSTRAINT PK_FavoriteBooks PRIMARY KEY AUTOINCREMENT,
                    BookId INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Author TEXT NOT NULL,
                    Genre TEXT NOT NULL,
                    Language TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL
                );");

            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_FavoriteBooks_BookId ON FavoriteBooks (BookId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FavoriteBooks_CreatedAtUtc ON FavoriteBooks (CreatedAtUtc DESC);");
        }

        private static void EnsureIsListenedColumn(AuralIndexDbContext db)
        {
            using var connection = db.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(Books);";

            using var reader = command.ExecuteReader();
            var hasColumn = false;

            while (reader.Read())
            {
                if (string.Equals(reader[1]?.ToString(), "IsListened", StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }

            if (!hasColumn)
            {
                reader.Close();
                using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE Books ADD COLUMN IsListened INTEGER NOT NULL DEFAULT 0;";
                alter.ExecuteNonQuery();
            }
        }

        private static void NormalizeLegacyLanguageCodes(AuralIndexDbContext db)
        {
            var changed = false;

            foreach (var book in db.Books)
            {
                var normalized = book.Language.Trim().ToLowerInvariant() switch
                {
                    "uk" => "Українська",
                    "en" => "Англійська",
                    "de" => "Німецька",
                    "fr" => "Французька",
                    "pl" => "Польська",
                    _ => book.Language
                };

                if (book.Language != normalized)
                {
                    book.Language = normalized;
                    changed = true;
                }
            }

            if (changed)
            {
                db.SaveChanges();
            }
        }

        private static void EnsureGenreCoverage(AuralIndexDbContext db)
        {
            var catalog = BuildSeedCatalog();
            var existingKeys = db.Books
                .AsEnumerable()
                .Select(book => CreateKey(book.Title, book.Author))
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

            var booksToAdd = new List<Book>();

            foreach (var group in catalog.GroupBy(book => book.Genre))
            {
                var currentCount = db.Books.Count(book => book.Genre == group.Key);
                var missing = Math.Max(0, 5 - currentCount);

                foreach (var candidate in group)
                {
                    if (missing == 0)
                    {
                        break;
                    }

                    var key = CreateKey(candidate.Title, candidate.Author);
                    if (existingKeys.Contains(key))
                    {
                        continue;
                    }

                    booksToAdd.Add(candidate);
                    existingKeys.Add(key);
                    missing--;
                }
            }

            if (booksToAdd.Count > 0)
            {
                db.Books.AddRange(booksToAdd);
                db.SaveChanges();
            }
        }

        private static List<Book> BuildSeedCatalog()
        {
            return new List<Book>
            {
                new() { Title = "1984", Author = "George Orwell", Genre = "Антиутопія", Language = "Англійська", Year = 1949, DurationMinutes = 640, Narrator = "Simon Prebble", Description = "Класичний антиутопічний роман про тоталітарне суспільство.", IsListened = true },
                new() { Title = "Brave New World", Author = "Aldous Huxley", Genre = "Антиутопія", Language = "Англійська", Year = 1932, DurationMinutes = 510, Narrator = "Michael York", Description = "Модель контрольованого суспільства майбутнього.", IsListened = false },
                new() { Title = "Fahrenheit 451", Author = "Ray Bradbury", Genre = "Антиутопія", Language = "Англійська", Year = 1953, DurationMinutes = 330, Narrator = "Tim Robbins", Description = "Світ, у якому книги заборонені, а пожежники їх спалюють.", IsListened = true },
                new() { Title = "Ми", Author = "Євген Замятін", Genre = "Антиутопія", Language = "Українська", Year = 1924, DurationMinutes = 430, Narrator = "Олександр Завальський", Description = "Роман-попередження про абсолютний контроль держави.", IsListened = false },
                new() { Title = "The Handmaid's Tale", Author = "Margaret Atwood", Genre = "Антиутопія", Language = "Англійська", Year = 1985, DurationMinutes = 610, Narrator = "Claire Danes", Description = "Антиутопія про обмеження свободи та владу ідеології.", IsListened = false },

                new() { Title = "Steve Jobs", Author = "Walter Isaacson", Genre = "Біографія", Language = "Англійська", Year = 2011, DurationMinutes = 900, Narrator = "Dylan Baker", Description = "Біографія засновника Apple.", IsListened = true },
                new() { Title = "Long Walk to Freedom", Author = "Nelson Mandela", Genre = "Біографія", Language = "Англійська", Year = 1994, DurationMinutes = 1080, Narrator = "Nelson Mandela", Description = "Автобіографія Нельсона Мандели.", IsListened = false },
                new() { Title = "Becoming", Author = "Michelle Obama", Genre = "Біографія", Language = "Англійська", Year = 2018, DurationMinutes = 1140, Narrator = "Michelle Obama", Description = "Особиста історія колишньої першої леді США.", IsListened = false },
                new() { Title = "Ілон Маск", Author = "Ashlee Vance", Genre = "Біографія", Language = "Українська", Year = 2015, DurationMinutes = 780, Narrator = "Павло Скороходько", Description = "Біографія підприємця та інноватора Ілона Маска.", IsListened = true },
                new() { Title = "The Diary of a Young Girl", Author = "Anne Frank", Genre = "Біографія", Language = "Англійська", Year = 1947, DurationMinutes = 470, Narrator = "Selma Blair", Description = "Щоденникові записи Анни Франк.", IsListened = false },

                new() { Title = "Sherlock Holmes", Author = "Arthur Conan Doyle", Genre = "Детектив", Language = "Англійська", Year = 1892, DurationMinutes = 710, Narrator = "Stephen Fry", Description = "Класичні детективні пригоди Шерлока Холмса.", IsListened = true },
                new() { Title = "The Hound of the Baskervilles", Author = "Arthur Conan Doyle", Genre = "Детектив", Language = "Англійська", Year = 1902, DurationMinutes = 420, Narrator = "David Timson", Description = "Одна з найвідоміших справ Шерлока Холмса.", IsListened = false },
                new() { Title = "Murder on the Orient Express", Author = "Agatha Christie", Genre = "Детектив", Language = "Англійська", Year = 1934, DurationMinutes = 380, Narrator = "Dan Stevens", Description = "Розслідування Еркюля Пуаро у поїзді.", IsListened = false },
                new() { Title = "The Girl with the Dragon Tattoo", Author = "Stieg Larsson", Genre = "Детектив", Language = "Англійська", Year = 2005, DurationMinutes = 960, Narrator = "Simon Vance", Description = "Скандинавський детектив з елементами трилера.", IsListened = true },
                new() { Title = "Ім'я троянди", Author = "Umberto Eco", Genre = "Детектив", Language = "Українська", Year = 1980, DurationMinutes = 950, Narrator = "Володимир Голосняк", Description = "Історичний детектив у монастирі.", IsListened = false },

                new() { Title = "The Book Thief", Author = "Markus Zusak", Genre = "Історична проза", Language = "Англійська", Year = 2005, DurationMinutes = 760, Narrator = "Allan Corduner", Description = "Історія дівчинки в часи Другої світової війни.", IsListened = false },
                new() { Title = "Wolf Hall", Author = "Hilary Mantel", Genre = "Історична проза", Language = "Англійська", Year = 2009, DurationMinutes = 820, Narrator = "Simon Slater", Description = "Роман про Томаса Кромвеля при дворі Генріха VIII.", IsListened = false },
                new() { Title = "All the Light We Cannot See", Author = "Anthony Doerr", Genre = "Історична проза", Language = "Англійська", Year = 2014, DurationMinutes = 760, Narrator = "Zach Appelman", Description = "Долі героїв на тлі війни в Європі.", IsListened = true },
                new() { Title = "The Pillars of the Earth", Author = "Ken Follett", Genre = "Історична проза", Language = "Англійська", Year = 1989, DurationMinutes = 1210, Narrator = "John Lee", Description = "Епічний роман про середньовічну Англію.", IsListened = false },
                new() { Title = "Захар Беркут", Author = "Іван Франко", Genre = "Історична проза", Language = "Українська", Year = 1883, DurationMinutes = 390, Narrator = "Петро Бойко", Description = "Історична повість про боротьбу громади з монголами.", IsListened = true },

                new() { Title = "Pride and Prejudice", Author = "Jane Austen", Genre = "Класика", Language = "Англійська", Year = 1813, DurationMinutes = 670, Narrator = "Rosamund Pike", Description = "Класичний роман про кохання та суспільство.", IsListened = true },
                new() { Title = "Jane Eyre", Author = "Charlotte Bronte", Genre = "Класика", Language = "Англійська", Year = 1847, DurationMinutes = 820, Narrator = "Thandie Newton", Description = "Історія гувернантки Джейн Ейр.", IsListened = false },
                new() { Title = "Moby-Dick", Author = "Herman Melville", Genre = "Класика", Language = "Англійська", Year = 1851, DurationMinutes = 1290, Narrator = "William Hootkins", Description = "Морська епопея про полювання на білого кита.", IsListened = false },
                new() { Title = "Великий Гетсбі", Author = "F. Scott Fitzgerald", Genre = "Класика", Language = "Українська", Year = 1925, DurationMinutes = 310, Narrator = "Сергій Кияшко", Description = "Американська класика про мрію та розчарування.", IsListened = true },
                new() { Title = "Три товариші", Author = "Erich Maria Remarque", Genre = "Класика", Language = "Українська", Year = 1936, DurationMinutes = 780, Narrator = "Олексій Комаровський", Description = "Роман про дружбу, кохання і післявоєнний світ.", IsListened = false },

                new() { Title = "Dune", Author = "Frank Herbert", Genre = "Наукова фантастика", Language = "Англійська", Year = 1965, DurationMinutes = 1260, Narrator = "Scott Brick", Description = "Епічна сага про планету Арракіс.", IsListened = true },
                new() { Title = "Foundation", Author = "Isaac Asimov", Genre = "Наукова фантастика", Language = "Англійська", Year = 1951, DurationMinutes = 510, Narrator = "William Hope", Description = "Початок знаменитого циклу про Галактичну імперію.", IsListened = false },
                new() { Title = "Neuromancer", Author = "William Gibson", Genre = "Наукова фантастика", Language = "Англійська", Year = 1984, DurationMinutes = 620, Narrator = "Robertson Dean", Description = "Кіберпанковий роман про штучний інтелект і мережі.", IsListened = false },
                new() { Title = "The Martian", Author = "Andy Weir", Genre = "Наукова фантастика", Language = "Англійська", Year = 2011, DurationMinutes = 660, Narrator = "R. C. Bray", Description = "Історія виживання астронавта на Марсі.", IsListened = true },
                new() { Title = "Соляріс", Author = "Stanisław Lem", Genre = "Наукова фантастика", Language = "Українська", Year = 1961, DurationMinutes = 520, Narrator = "Андрій Соболев", Description = "Філософська фантастика про контакт із незбагненним.", IsListened = false },

                new() { Title = "Sapiens", Author = "Yuval Noah Harari", Genre = "Науково-популярна", Language = "Англійська", Year = 2011, DurationMinutes = 900, Narrator = "Derek Perkins", Description = "Коротка історія людства.", IsListened = true },
                new() { Title = "A Brief History of Time", Author = "Stephen Hawking", Genre = "Науково-популярна", Language = "Англійська", Year = 1988, DurationMinutes = 360, Narrator = "Erik Davies", Description = "Пояснення складних космологічних ідей простою мовою.", IsListened = false },
                new() { Title = "The Selfish Gene", Author = "Richard Dawkins", Genre = "Науково-популярна", Language = "Англійська", Year = 1976, DurationMinutes = 660, Narrator = "Richard Dawkins", Description = "Популярна книга з еволюційної біології.", IsListened = false },
                new() { Title = "Cosmos", Author = "Carl Sagan", Genre = "Науково-популярна", Language = "Англійська", Year = 1980, DurationMinutes = 840, Narrator = "LeVar Burton", Description = "Подорож історією науки та Всесвіту.", IsListened = true },
                new() { Title = "Як влаштовані майже всі речі", Author = "Bill Bryson", Genre = "Науково-популярна", Language = "Українська", Year = 2003, DurationMinutes = 760, Narrator = "Юрій Табаченко", Description = "Доступна книга про науку, Всесвіт і Землю.", IsListened = false },

                new() { Title = "Thinking, Fast and Slow", Author = "Daniel Kahneman", Genre = "Психологія", Language = "Англійська", Year = 2011, DurationMinutes = 820, Narrator = "Patrick Egan", Description = "Книга про два режими мислення.", IsListened = true },
                new() { Title = "Atomic Habits", Author = "James Clear", Genre = "Психологія", Language = "Англійська", Year = 2018, DurationMinutes = 340, Narrator = "James Clear", Description = "Практика формування корисних звичок.", IsListened = true },
                new() { Title = "The Power of Habit", Author = "Charles Duhigg", Genre = "Психологія", Language = "Англійська", Year = 2012, DurationMinutes = 620, Narrator = "Mike Chamberlain", Description = "Пояснення механізмів звичок та їх зміни.", IsListened = false },
                new() { Title = "Man's Search for Meaning", Author = "Viktor Frankl", Genre = "Психологія", Language = "Англійська", Year = 1946, DurationMinutes = 300, Narrator = "Simon Vance", Description = "Психологічний і філософський твір про сенс життя.", IsListened = false },
                new() { Title = "Емоційний інтелект", Author = "Daniel Goleman", Genre = "Психологія", Language = "Українська", Year = 1995, DurationMinutes = 690, Narrator = "Олег Терновий", Description = "Книга про важливість керування емоціями.", IsListened = true },

                new() { Title = "Тигролови", Author = "Іван Багряний", Genre = "Роман", Language = "Українська", Year = 1944, DurationMinutes = 520, Narrator = "Василь Чорношкур", Description = "Український пригодницький роман.", IsListened = true },
                new() { Title = "The Night Circus", Author = "Erin Morgenstern", Genre = "Роман", Language = "Англійська", Year = 2011, DurationMinutes = 640, Narrator = "Jim Dale", Description = "Атмосферний роман про магію і суперництво.", IsListened = false },
                new() { Title = "Normal People", Author = "Sally Rooney", Genre = "Роман", Language = "Англійська", Year = 2018, DurationMinutes = 430, Narrator = "Aoife McMahon", Description = "Сучасний роман про стосунки та дорослішання.", IsListened = false },
                new() { Title = "Atonement", Author = "Ian McEwan", Genre = "Роман", Language = "Англійська", Year = 2001, DurationMinutes = 760, Narrator = "Jill Tanner", Description = "Історія кохання, провини та спокути.", IsListened = true },
                new() { Title = "The Goldfinch", Author = "Donna Tartt", Genre = "Роман", Language = "Англійська", Year = 2013, DurationMinutes = 1090, Narrator = "David Pittu", Description = "Великий роман про втрату, мистецтво й дорослішання.", IsListened = false },

                new() { Title = "The Hobbit", Author = "J. R. R. Tolkien", Genre = "Фентезі", Language = "Англійська", Year = 1937, DurationMinutes = 680, Narrator = "Andy Serkis", Description = "Подорож Більбо Беґінса до Самотньої гори.", IsListened = true },
                new() { Title = "Harry Potter and the Philosopher's Stone", Author = "J. K. Rowling", Genre = "Фентезі", Language = "Англійська", Year = 1997, DurationMinutes = 520, Narrator = "Stephen Fry", Description = "Початок магічної саги про Гаррі Поттера.", IsListened = true },
                new() { Title = "The Name of the Wind", Author = "Patrick Rothfuss", Genre = "Фентезі", Language = "Англійська", Year = 2007, DurationMinutes = 1670, Narrator = "Nick Podehl", Description = "Історія мага Квоута.", IsListened = false },
                new() { Title = "A Game of Thrones", Author = "George R. R. Martin", Genre = "Фентезі", Language = "Англійська", Year = 1996, DurationMinutes = 2010, Narrator = "Roy Dotrice", Description = "Перша книга епічної саги про Вестерос.", IsListened = false },
                new() { Title = "Відьмак. Останнє бажання", Author = "Andrzej Sapkowski", Genre = "Фентезі", Language = "Українська", Year = 1993, DurationMinutes = 600, Narrator = "Євген Малуха", Description = "Збірка історій про відьмака Геральта.", IsListened = true }
            };
        }

        private static string CreateKey(string title, string author)
        {
            return $"{title.Trim()}|{author.Trim()}";
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Shell = new MainWindow();
            _window = Shell;
            _window.Activate();
        }
    }
}
