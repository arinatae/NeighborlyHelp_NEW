using NeighborlyHelp.Managers;
using NeighborlyHelp.Models;
using System.Reflection;

---NeighborlyHelp / Models / GameModel.cs(原始)


++ + NeighborlyHelp / Models / GameModel.cs(修改后)
using System.Collections.Generic;
using System.Drawing;

namespace NeighborlyHelp.Models
{
    /// <summary>
    /// Модель данных игры (Model в MVC).
    /// Хранит всё состояние игры: игрока, объекты, NPC, инвентарь, квесты и т.д.
    /// Не содержит логики отображения или обработки ввода.
    /// </summary>
    public class GameModel
    {
        // === Состояние игры ===
        public GameState CurrentState { get; set; } = GameState.Intro;

        // === Игровые сущности ===
        public Player Player { get; set; } = null!;
        public GameField GameField { get; set; } = null!;
        public Inventory Inventory { get; set; } = new Inventory();
        public QuestManager QuestManager { get; set; } = new QuestManager();

        // === Списки объектов ===
        public List<GameObject> GameObjects { get; set; } = new List<GameObject>();
        public List<NPC> NPCs { get; set; } = new List<NPC>();
        public List<Collectible> Collectibles { get; set; } = new List<Collectible>();

        // === Мини-игры ===
        public List<MailBoxOption> MailOptions { get; set; } = new List<MailBoxOption>();
        public List<FlowerData> Flowers { get; set; } = new List<FlowerData>();

        // === Флаги состояния ===
        public bool IsMiniGameActive { get; set; } = false;
        public bool IsFlowerGameActive { get; set; } = false;
        public bool IsRadioGameActive { get; set; } = false;
        public bool IsWatering { get; set; } = false;
        public bool IsDraggingRadio { get; set; } = false;

        // === Радио мини-игра ===
        public float RadioFreq { get; set; } = 88.0f;
        public float TargetFreq { get; set; } = 95.5f;
        public Rectangle RadioBarBounds { get; set; }

        // === Позиция полива ===
        public Point WateringPos { get; set; } = Point.Empty;

        // === Подсказка взаимодействия ===
        public string InteractionHint { get; set; } = "";

        // === Константы ===
        public const int INTERACTION_RADIUS = 120;

        /// <summary>
        /// Проверяет расстояние между КРАЯМИ персонажа и цели.
        /// </summary>
        public bool IsCloseTo(Rectangle targetBounds)
        {
            Rectangle playerRect = new Rectangle(Player.X, Player.Y, Player.Width, Player.Height);

            // Вычисляем разрыв по горизонтали и вертикали
            int dx = Math.Max(0, Math.Max(playerRect.Left - targetBounds.Right, targetBounds.Left - playerRect.Right));
            int dy = Math.Max(0, Math.Max(playerRect.Top - targetBounds.Bottom, targetBounds.Top - playerRect.Bottom));

            // Если персонажи пересекаются или касаются, dx и dy будут равны 0
            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance <= INTERACTION_RADIUS;
        }

        /// <summary>
        /// Инициализирует игровое поле и создаёт начальные объекты.
        /// </summary>
        public void Initialize()
        {
            GameField = new GameField();
            Player = new Player(530, 450);
            Player.Width = 200;
            Player.Height = 200;

            // Добавляем деревья
            GameObjects.Add(new Tree(225, 15));
            GameObjects.Add(new Tree(800, 150));
            GameObjects.Add(new Tree(500, 800));
            GameObjects.Add(new Tree(1200, 730));

            // Добавляем скамейки
            GameObjects.Add(new Bench(800, 700));
            GameObjects.Add(new Bench(100, 330));

            // Добавляем клумбу
            GameObjects.Add(new FlowerBed(40, 450));

            // Добавляем почтовый ящик
            GameObjects.Add(new Mailbox(1150, 45));

            // Добавляем стены (границы)
            GameObjects.Add(new Wall(0, 0, GameField.Width, 10));
            GameObjects.Add(new Wall(0, GameField.Height - 10, GameField.Width, 10));
            GameObjects.Add(new Wall(0, 0, 10, GameField.Height));
            GameObjects.Add(new Wall(GameField.Width - 10, 0, 10, GameField.Height));
        }

        /// <summary>
        /// Обновляет границы поля при изменении размера окна.
        /// </summary>
        public void ResizeField(int width, int height)
        {
            if (GameField == null) return;

            GameField.Width = width;
            GameField.Height = height;

            // Удаляем старые стены и создаём новые
            GameObjects.RemoveAll(obj => obj is Wall);
            GameObjects.Add(new Wall(0, 0, GameField.Width, 10));
            GameObjects.Add(new Wall(0, GameField.Height - 10, GameField.Width, 10));
            GameObjects.Add(new Wall(0, 0, 10, GameField.Height));
            GameObjects.Add(new Wall(GameField.Width - 10, 0, 10, GameField.Height));
        }

        /// <summary>
        /// Создаёт ключи как подбираемый предмет.
        /// </summary>
        public void SpawnKeys()
        {
            if (Collectibles.Exists(c => c.Item.Name == "Ключи" && !c.IsPickedUp))
                return;

            Item keyItem = new Item("Ключи", "Блестящие ключи от домика", Color.Gold);
            Collectible keys = new Collectible(310, 460, keyItem, "spritekey.png");
            Collectibles.Add(keys);
            GameObjects.Add(keys);
        }

        /// <summary>
        /// Создаёт NPC на поле.
        /// </summary>
        public void SpawnNPC(string name, int x, int y, List<string> lines, string spriteName, int width, int height, string portraitFile = "")
        {
            NPC newNpc = new NPC(x, y, name, lines, spriteName, width, height, portraitFile);
            NPCs.Add(newNpc);
            GameObjects.Add(newNpc);
        }

        /// <summary>
        /// Удаляет NPC по имени.
        /// </summary>
        public void RemoveNPC(string name)
        {
            var npc = NPCs.Find(n => n.DisplayName == name);
            if (npc != null)
            {
                GameObjects.Remove(npc);
                NPCs.Remove(npc);
            }
        }

        /// <summary>
        /// Запускает мини-игру с почтовыми ящиками.
        /// </summary>
        public void StartMailboxMiniGame(int clientWidth, int clientHeight)
        {
            MailOptions.Clear();
            Random rnd = new Random();
            int correctIndex = rnd.Next(0, 50);

            int cols = 10, rows = 5, boxSize = 100, gap = 20;
            int totalWidth = cols * (boxSize + gap) - gap;
            int totalHeight = rows * (boxSize + gap) - gap;
            int startX = (clientWidth - totalWidth) / 2;
            int startY = (clientHeight - totalHeight) / 2 + 30;

            for (int i = 0; i < 50; i++)
            {
                int row = i / cols, col = i % cols;
                int x = startX + col * (boxSize + gap);
                int y = startY + row * (boxSize + gap);
                string number = (i == correctIndex) ? "18046" : rnd.Next(10000, 99999).ToString();

                MailOptions.Add(new MailBoxOption
                {
                    Bounds = new Rectangle(x, y, boxSize, boxSize),
                    Number = number,
                    IsCorrect = (i == correctIndex)
                });
            }

            IsMiniGameActive = true;
            CurrentState = GameState.Quest2_MiniGame;
        }

        /// <summary>
        /// Запускает мини-игру с поливом цветов.
        /// </summary>
        public void StartFlowerMiniGame(int clientWidth, int clientHeight)
        {
            Flowers.Clear();

            int cols = 5;
            int rows = 3;
            int cellSize = 100;

            int totalW = cols * cellSize;
            int totalH = rows * cellSize;

            int startX = (clientWidth - totalW) / 2;
            int startY = (clientHeight - totalH) / 2 - 50;

            for (int i = 0; i < 15; i++)
            {
                int r = i / cols;
                int c = i % cols;

                Flowers.Add(new FlowerData
                {
                    Bounds = new Rectangle(startX + c * cellSize, startY + r * cellSize, cellSize, cellSize)
                });
            }

            IsFlowerGameActive = true;
            CurrentState = GameState.Quest3_Watering;
        }

        /// <summary>
        /// Запускает мини-игру с радио.
        /// </summary>
        public void StartRadioMiniGame(int clientWidth, int clientHeight)
        {
            IsRadioGameActive = true;
            RadioFreq = 88.0f;
            TargetFreq = 88.0f + (float)(new Random().NextDouble() * 15);
            CurrentState = GameState.Quest4_Radio;

            RadioBarBounds = new Rectangle(
                (clientWidth - 400) / 2,
                clientHeight / 2 - 20,
                400, 40
            );
        }

        /// <summary>
        /// Обновляет частоту радио на основе позиции мыши.
        /// </summary>
        public void UpdateRadioFreq(int mouseX)
        {
            float ratio = (mouseX - RadioBarBounds.X) / (float)RadioBarBounds.Width;
            RadioFreq = 88.0f + ratio * 20.0f;
            RadioFreq = Math.Max(88.0f, Math.Min(108.0f, RadioFreq));
        }

        /// <summary>
        /// Поливает цветы в мини-игре.
        /// </summary>
        public void WaterFlowers()
        {
            if (!IsFlowerGameActive || !IsWatering) return;

            foreach (var f in Flowers)
            {
                if (!f.IsFull && f.Bounds.Contains(WateringPos))
                {
                    f.WaterLevel += 4;
                    if (f.WaterLevel > 100) f.WaterLevel = 100;
                }
            }
        }

        /// <summary>
        /// Проверяет победу в мини-игре с цветами.
        /// </summary>
        public bool CheckFlowerGameWin()
        {
            if (IsFlowerGameActive && Flowers.All(f => f.IsFull))
            {
                IsFlowerGameActive = false;
                IsWatering = false;
                CurrentState = GameState.Quest3_Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Проверяет победу в мини-игре с радио.
        /// </summary>
        public bool CheckRadioGameWin()
        {
            if (IsRadioGameActive && Math.Abs(RadioFreq - TargetFreq) <= 0.8f)
            {
                IsRadioGameActive = false;
                CurrentState = GameState.Quest4_Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Движение игрока с проверкой коллизий.
        /// </summary>
        public bool TryMovePlayer(int newX, int newY)
        {
            if (GameField == null) return false;

            // Проверка границ
            if (newX < 0 || newX > GameField.Width - Player.Width ||
                newY < 0 || newY > GameField.Height - Player.Height)
                return false;

            Rectangle futureRect = new Rectangle(newX, newY, Player.Width, Player.Height);
            foreach (var obj in GameObjects)
            {
                if (obj.IsSolid && futureRect.IntersectsWith(obj.Bounds))
                    return false;
            }

            Player.X = newX;
            Player.Y = newY;
            return true;
        }
    }

    /// <summary>
    /// Данные цветка для мини-игры.
    /// </summary>
    public class FlowerData
    {
        public Rectangle Bounds { get; set; }
        public int WaterLevel { get; set; } = 0;
        public bool IsFull => WaterLevel >= 100;
    }

    /// <summary>
    /// Опция почтового ящика для мини-игры.
    /// </summary>
    public class MailBoxOption
    {
        public Rectangle Bounds { get; set; }
        public string Number { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}