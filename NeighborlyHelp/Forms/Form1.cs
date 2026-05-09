using NeighborlyHelp.Managers;
using NeighborlyHelp.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Screen = System.Windows.Forms.Screen;

namespace NeighborlyHelp
{
    public partial class Form1 : Form
    {
        private Models.GameState currentGameState = Models.GameState.Intro;
        private Player player = null!;
        private GameField gameField = null!;
        private Timer gameTimer = null!;
        private List<GameObject> gameObjects = new List<GameObject>();
        private List<NPC> npcs = new List<NPC>();
        private Inventory inventory = new Inventory();
        private List<MailBoxOption> mailOptions = new List<MailBoxOption>();
        private bool isMiniGameActive = false;
        private QuestManager questManager = new QuestManager();
        private List<Collectible> collectibles = new List<Collectible>();
        private Bitmap? playerSprite;
        private Bitmap? backgroundImage;
        private Bitmap? boxSprite;
        private Bitmap? flowerSprite; // Спрайт цветка для мини-игры
        private bool isDialogueActive = false;
        private string dialogueSpeaker = "";
        private List<string> dialogueLines = new List<string>();
        private int dialogueLineIndex = 0;
        private Bitmap? dialogueSprite = null;
        private string playerDisplayName = "Ты";
        private Bitmap? playerPortrait = null;
        // Поля для мини-игры с цветами
        private bool isFlowerGameActive = false;
        private List<FlowerData> flowers = new List<FlowerData>();
        private bool isWatering = false;
        private Point wateringPos = Point.Empty;
        // Поля для мини-игры с радио
        private bool isRadioGameActive = false;
        private float radioFreq = 88.0f;      // Текущая частота (88.0 - 108.0)
        private float targetFreq = 95.5f;     // Цель
        private bool isDraggingRadio = false; // Зажат ли ползунок
        private Rectangle radioBarBounds;     // Границы панели настройки

        // Класс для одного цветка
        public class FlowerData
        {
            public Rectangle Bounds { get; set; }
            public int WaterLevel { get; set; } = 0; // 0..100
            public bool IsFull => WaterLevel >= 100;
        }
        private const int INTERACTION_RADIUS = 120; // Радиус взаимодействия (в пикселях)
        private string interactionHint = "";          // Текст подсказки на экране
        private Timer hintTimer = new Timer(); // Таймер для авто-скрытия подсказки


        // Метод проверяет расстояние между КРАЯМИ персонажа и цели
        private bool IsCloseTo(Rectangle targetBounds)
        {
            Rectangle playerRect = new Rectangle(player.X, player.Y, player.Width, player.Height);

            // Вычисляем разрыв по горизонтали и вертикали
            int dx = Math.Max(0, Math.Max(playerRect.Left - targetBounds.Right, targetBounds.Left - playerRect.Right));
            int dy = Math.Max(0, Math.Max(playerRect.Top - targetBounds.Bottom, targetBounds.Top - playerRect.Bottom));

            // Если персонажи пересекаются или касаются, dx и dy будут равны 0
            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance <= INTERACTION_RADIUS;
        }

        private void HintTimer_Tick(object sender, EventArgs e)
        {
            interactionHint = "";
            hintTimer.Stop();
            this.Invalidate(); // Перерисовка, чтобы убрать надпись
        }

        public Form1()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;
            this.MouseClick += Form1_MouseClick;

            this.Text = "🏡 Соседская помощь";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;
            this.Size = Screen.PrimaryScreen.Bounds.Size;
            this.Location = new Point(0, 0);
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = ColorTranslator.FromHtml("#87CEEB");
            this.KeyPreview = true;

            gameField = new GameField();
            player = new Player(530, 450);
            player.Width = 201;  // Размер игрока
            player.Height = 200;

            try { playerSprite = new Bitmap("Assets/sprite0.png"); }
            catch { playerSprite = null; }

            try
            {
                backgroundImage = new Bitmap("Assets/backpicture.png");
                backgroundImage = new Bitmap(backgroundImage, gameField.Width, gameField.Height);
            }
            catch { backgroundImage = null; }

            try { boxSprite = new Bitmap("Assets/sprite-box.png"); }
            catch { boxSprite = null; }

            try { flowerSprite = new Bitmap("Assets/spriteflower.png"); }
            catch { flowerSprite = null; }
            try { playerPortrait = new Bitmap("Assets/portrait0.png"); }
            catch { playerPortrait = null; }
            gameObjects.Add(new Tree(225, 15));
            gameObjects.Add(new Tree(800, 150));
            gameObjects.Add(new Tree(500, 800));
            gameObjects.Add(new Tree(1200, 730));

            gameObjects.Add(new Bench(800, 700));
            gameObjects.Add(new Bench(100, 330));

            gameObjects.Add(new FlowerBed(40, 450));

            gameObjects.Add(new Mailbox(1150, 45));

            gameObjects.Add(new Wall(0, 0, gameField.Width, 10));
            gameObjects.Add(new Wall(0, gameField.Height - 10, gameField.Width, 10));
            gameObjects.Add(new Wall(0, 0, 10, gameField.Height));
            gameObjects.Add(new Wall(gameField.Width - 10, 0, 10, gameField.Height));

            gameTimer = new Timer { Interval = 16 };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += Form1_KeyDown;

            hintTimer.Interval = 2000; // 2 секунды
            hintTimer.Tick += HintTimer_Tick;

            this.MouseDown += Form1_MouseDown;
            this.MouseUp += Form1_MouseUp;
            this.MouseMove += Form1_MouseMove;

            StartStory();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (isDialogueActive)
            {
                AdvanceDialogue();
                return;
            }

            if (isMiniGameActive)
            {
                foreach (var box in mailOptions)
                {
                    if (box.Bounds.Contains(e.X, e.Y))
                    {
                        if (box.IsCorrect)
                        {
                            MessageBox.Show("Посылка №18046 найдена! Отнеси её Оливеру.", "Успех");
                            inventory.Add(new Item("Посылка №18046", "Тяжелая коробка", Color.Brown));
                            isMiniGameActive = false;
                            mailOptions.Clear();
                            currentGameState = Models.GameState.Quest2_Deliver;
                            this.Invalidate();
                        }
                        else
                        {
                            MessageBox.Show("Не та коробка! Ищи посылку №18046.", "Ошибка");
                        }
                        return;
                    }
                }
                return;
            }

            if (currentGameState == Models.GameState.Quest2_Spawn)
            {
                foreach (var obj in gameObjects)
                {
                    if (obj is Mailbox && obj.Bounds.Contains(e.X, e.Y))
                    {
                        StartMailboxMiniGame();
                        return;
                    }
                }
            }

            // === КЛИК ПО КЛУМБЕ (Запуск мини-игры) ===
            if (currentGameState == Models.GameState.Quest3_Spawn)
            {
                foreach (var obj in gameObjects)
                {
                    if (obj is FlowerBed && obj.Bounds.Contains(e.X, e.Y))
                    {
                        StartFlowerMiniGame();
                        return;
                    }
                }
            }

            // === КЛИК ПО РАДИО (Запуск мини-игры) ===
            if (currentGameState == Models.GameState.Quest4_Talk)
            {
                foreach (var obj in gameObjects)
                {
                    if (obj is Radio && obj.Bounds.Contains(e.X, e.Y))
                    {
                        StartRadioMiniGame();
                        return;
                    }
                }
            }

            foreach (var item in collectibles)
            {
                if (!item.IsPickedUp && item.Bounds.Contains(e.X, e.Y))
                {
                    if (!IsCloseTo(item.Bounds))
                    {
                        interactionHint = "Подойдите ближе!";
                        hintTimer.Stop(); hintTimer.Start(); // ⏱ Запуск таймера
                        this.Invalidate();
                        return;
                    }

                    item.IsPickedUp = true;
                    inventory.Add(item.Item);
                    interactionHint = ""; // Убираем подсказку при успехе

                    if (currentGameState == Models.GameState.Quest1_Find && item.Item.Name == "Ключи")
                    {
                        MessageBox.Show("Нашёл ключи! Отнеси их Миле.", "Находка");
                        currentGameState = Models.GameState.Quest1_Return;
                    }
                    return;
                }
            }

            foreach (var npc in npcs)
            {
                if (npc.IsDialogAvailable && npc.Bounds.Contains(e.X, e.Y))
                {
                    if (!IsCloseTo(npc.Bounds))
                    {
                        interactionHint = "Подойдите ближе!";
                        hintTimer.Stop(); hintTimer.Start();
                        this.Invalidate();
                        return;
                    }

                    List<string> linesToSay = npc.DialogLines;
                    List<string> playerLines = new List<string>(); // Ответы игрока по умолчанию пустые
                    string spriteName = "sprite1.png";

                    if (npc.DisplayName == "Мила")
                    {
                        spriteName = "sprite1.png";
                        if (currentGameState == Models.GameState.Quest1_Return)
                        {
                            linesToSay = new List<string>
                {
                    "О, что это? Ты нашла мои ключики! Теперь я могу спокойно зайти домой",
                    "Спасибо тебе большое! Я буду аккуратнее обращаться со своими вещами. Приходи ко мне на чай сегодня вечером!",
                    "Да, посиделки нашей дружной компанией - это прекрасно! Кстати, здесь только что пробегал запыхавшийся Оливер"
                };
                            playerLines = new List<string>
                {
                    "Вот, держи свои ключи! Больше не теряй, будь внимательна и всегда следи за своими вещами!",
                    "С удовольствием приду! Мы можем позвать на чаепитие всех соседей. А пока я найду еще кого-нибудь",
                    "Ха-ха, не удивлена! Он вечно куда-то спешит. Пойду найду его, может быть смогу чем-то помочь"
                };
                        }
                        else // Начальный диалог
                        {
                            playerLines = new List<string>
                {
                    "Привет, Мила! Да, у меня все прекрасно. Вот вышла на прогулку, подышать свежим воздухом и заняться чем-нибудь интересным. Как твои дела?",
                    "Как же так! Наверняка ты их просто где-то выронила. Давай мы найдем их вместе!"
                };
                        }
                    }
                    else if (npc.DisplayName == "Оливер")
                    {
                        spriteName = "sprite2.png";
                        if (currentGameState == Models.GameState.Quest2_Deliver)
                        {
                            linesToSay = new List<string>
                {
                    "Ты уже вернулась? Даже забрала мою посылку! Супер, огромное тебе спасибо!",
                    "Ты такая хорошая соседка! Как всегда меня выручила в самый трудный момент. Я обязательно помогу тебе в ответ, когда это потребуется, только скажи!",
                    "Вау, круто! Да, знаешь, кажется я с утра видел Мелиссу. Она сказала мне, что хочет заняться цветами на клумбе"
                };
                            playerLines = new List<string>
                {
                    "Здравствуйте, курьер-соседка к Вашим услугам, ха-ха! Заказ 18046 твой!",
                    "Рада стараться! Сегодня вечером Мила пригласила всех на чаепитие. Может быть ты видел кого-то ещё из наших соседей?",
                    "Конечно, садоводство - её любимое занятие, как я сразу не догадалась! Тогда пррогуляюсь до нашей клумбы"
                };
                        }
                        else
                        {
                            playerLines = new List<string> { "Привет, Оливер! Чем могу помочь?", "Без проблем, сейчас схожу на почту." };
                        }
                    }
                    else if (npc.DisplayName == "Мелисса")
                    {
                        spriteName = "sprite1.png";
                        if (currentGameState == Models.GameState.Quest3_Completed)
                        {
                            linesToSay = new List<string>
                {
                    "Боже мой, клумба просто ожила! Спасибо тебе огромное!",
                    "Ты самая добрая соседка. Хочешь, подарю тебе букет?",
                    "Кстати, Ричард из четвёртого домика ждёт помощи у баков."
                };
                            playerLines = new List<string>
                {
                    "Цветы любят воду, всё просто! ",
                    "Спасибо, букет будет кстати!",
                    "Поняла, сейчас найду Ричарда."
                };
                        }
                        else
                        {
                            playerLines = new List<string> { "Привет, Мелисса! Красивые цветы.", "Конечно, помогу полить!" };
                        }
                    }
                    else if (npc.DisplayName == "Ричард")
                    {
                        spriteName = "sprite4.png";
                        if (currentGameState == Models.GameState.Quest4_Spawn)
                        {
                            linesToSay = new List<string>
                {
                    "Кто пришел? Ты от Мелиссы? Здорово! Слушай, у меня тут беда...",
                    "Я пытаюсь поймать подкаст о насекомых, но крутилка заела.",
                    "Помоги настроить частоту на 95.5 МГц. Двигай ползунок в зелёную зону!"
                };
                            playerLines = new List<string>
                {
                    "Да, она сказала, что тебе нужна помощь. Что стряслось?",
                    "Старое радио? Попробую починить.",
                    "Сейчас настрою, держись!"
                };
                        }
                        else if (currentGameState == Models.GameState.Quest4_Completed)
                        {
                            linesToSay = new List<string>
                {
                    "Спасибо тебе огромное! Подкаст заиграл!",
                    "Ты настоящая волшебница. Наш двор стал уютнее благодаря тебе!"
                };
                            playerLines = new List<string>
                {
                    "Всегда пожалуйста! Приятного прослушивания.",
                    "Рада, что помогла. Береги себя!"
                };
                        }
                        else
                        {
                            playerLines = new List<string> { "Привет, Ричард! Чем могу помочь?" };
                        }
                    }

                    StartDialogue(npc.DisplayName, linesToSay, playerLines, spriteName);
                    return;
                }
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (isFlowerGameActive)
            {
                isWatering = true;
                wateringPos = e.Location;
            }

            // === ДЛЯ РАДИО ===
            if (isRadioGameActive && radioBarBounds.Contains(e.Location))
            {
                isDraggingRadio = true;
                UpdateRadioFreq(e.X);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isFlowerGameActive) wateringPos = e.Location;

            // === ДЛЯ РАДИО ===
            if (isDraggingRadio)
            {
                UpdateRadioFreq(e.X);
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isFlowerGameActive) isWatering = false;

            // === ДЛЯ РАДИО ===
            if (isDraggingRadio)
            {
                isDraggingRadio = false;
            }
        }

        // === НОВЫЙ МЕТОД ===
        private void UpdateRadioFreq(int mouseX)
        {
            float ratio = (mouseX - radioBarBounds.X) / (float)radioBarBounds.Width;
            radioFreq = 88.0f + ratio * 20.0f;
            radioFreq = Math.Max(88.0f, Math.Min(108.0f, radioFreq));
            this.Invalidate();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            int oldX = player.X;
            int oldY = player.Y;

            player.X = Math.Max(0, Math.Min(player.X, gameField.Width - player.Width));
            player.Y = Math.Max(0, Math.Min(player.Y, gameField.Height - player.Height));

            Rectangle playerRect = new Rectangle(player.X, player.Y, player.Width, player.Height);
            foreach (var obj in gameObjects)
            {
                if (obj.IsSolid && playerRect.IntersectsWith(obj.Bounds))
                {
                    player.X = oldX;
                    player.Y = oldY;
                    break;
                }
            }

            // === ЛОГИКА МИНИ-ИГРЫ С ЦВЕТАМИ ===
            if (isFlowerGameActive && isWatering)
            {
                foreach (var f in flowers)
                {
                    if (!f.IsFull && f.Bounds.Contains(wateringPos))
                    {
                        f.WaterLevel += 4; // Скорость полива
                        if (f.WaterLevel > 100) f.WaterLevel = 100;
                    }
                }

                // Проверка победы
                if (flowers.All(f => f.IsFull))
                {
                    isFlowerGameActive = false;
                    isWatering = false;
                    currentGameState = Models.GameState.Quest3_Completed;
                    MessageBox.Show("🌸 Все цветы расцвели! Отличная работа!", "Успех");
                }
            }
            // Проверка победы в радио-игре
            if (isRadioGameActive && Math.Abs(radioFreq - targetFreq) <= 0.8f)
            {
                isRadioGameActive = false;
                currentGameState = Models.GameState.Quest4_Completed;
                MessageBox.Show($"📻 Частота {radioFreq:F1} МГц поймана! Передача идет!", "Успех");
                this.Invalidate();
            }
            this.Invalidate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int newX = player.X;
            int newY = player.Y;
            int speed = player.Speed;

            switch (e.KeyCode)
            {
                case Keys.W: case Keys.Up: newY -= speed; break;
                case Keys.S: case Keys.Down: newY += speed; break;
                case Keys.A: case Keys.Left: newX -= speed; break;
                case Keys.D: case Keys.Right: newX += speed; break;

                case Keys.Escape:
                    gameTimer.Stop();
                    var result = MessageBox.Show(
                        "Игра на паузе!\n\nВыберите действие:",
                        "Пауза",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes) gameTimer.Start();
                    else if (result == DialogResult.No) Application.Exit();
                    else gameTimer.Start();
                    return;

                case Keys.I:
                    MessageBox.Show($"🎒 Инвентарь:\n{inventory.GetList()}", "Инвентарь");
                    return;
            }

            if (newX < 0 || newX > gameField.Width - player.Width ||
                newY < 0 || newY > gameField.Height - player.Height)
                return;

            Rectangle futureRect = new Rectangle(newX, newY, player.Width, player.Height);
            foreach (var obj in gameObjects)
            {
                if (obj.IsSolid && futureRect.IntersectsWith(obj.Bounds))
                    return;
            }

            player.X = newX;
            player.Y = newY;
            this.Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (gameField == null || gameObjects == null) return;

            gameField.Width = this.ClientSize.Width;
            gameField.Height = this.ClientSize.Height;

            gameObjects.RemoveAll(obj => obj is Wall);
            gameObjects.Add(new Wall(0, 0, gameField.Width, 10));
            gameObjects.Add(new Wall(0, gameField.Height - 10, gameField.Width, 10));
            gameObjects.Add(new Wall(0, 0, 10, gameField.Height));
            gameObjects.Add(new Wall(gameField.Width - 10, 0, 10, gameField.Height));

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 1. Фон
            if (backgroundImage != null)
                e.Graphics.DrawImage(backgroundImage, 0, 0, gameField.Width, gameField.Height);
            else
                e.Graphics.Clear(BackColor);

            // 2. Объекты (деревья, скамейки, стены)
            foreach (var obj in gameObjects)
                obj.Draw(e.Graphics);

            // 3. Игрок
            if (playerSprite != null)
                e.Graphics.DrawImage(playerSprite, player.X, player.Y, player.Width, player.Height);

            // 4. ПОДСКАЗКА ВЗАИМОДЕЙСТВИЯ (Рисуется поверх игрока)
            if (!string.IsNullOrEmpty(interactionHint))
            {
                Font hintFont = new Font("Arial", 14, FontStyle.Bold);
                SizeF hintSize = e.Graphics.MeasureString(interactionHint, hintFont);

                // Позиция над головой игрока (по центру)
                float x = player.X + player.Width / 2 - hintSize.Width / 2;
                float y = player.Y - 30; // Чуть выше спрайта

                // Полупрозрачный фон для текста (чтобы было видно на любой траве)
                using (Brush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(bgBrush, x - 5, y - 2, hintSize.Width + 10, hintSize.Height + 4);
                }

                // Белый текст
                e.Graphics.DrawString(interactionHint, hintFont, Brushes.White, x, y);
            }

            // === МИНИ-ИГРА: ПОЛИВ ЦВЕТОВ ===
            if (isFlowerGameActive)
            {
                // Затемнение фона
                using (Brush overlay = new SolidBrush(Color.FromArgb(210, 10, 30, 10)))
                    e.Graphics.FillRectangle(overlay, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

                // Заголовок
                Font titleFont = new Font("Arial", 20, FontStyle.Bold);
                e.Graphics.DrawString("🌿 Полей все цветы из лейки", titleFont, Brushes.LightGreen,
                    new PointF((this.ClientSize.Width - 380) / 2, 40));

                // Рисуем каждый цветок
                foreach (var f in flowers)
                {
                    // 1. Рисуем PNG-спрайт (центрируем внутри ячейки, оставляя место снизу)
                    if (flowerSprite != null)
                    {
                        // Вычисляем размер картинки, чтобы она влезала в ячейку, но не была слишком маленькой
                        int drawW = f.Bounds.Width - 20;
                        int drawH = f.Bounds.Height - 40; // Оставляем место снизу для полосы
                        int drawX = f.Bounds.X + 10;
                        int drawY = f.Bounds.Y + 10;

                        e.Graphics.DrawImage(flowerSprite, drawX, drawY, drawW, drawH);
                    }
                    else
                    {
                        // Если нет картинки, рисуем зеленый круг как заглушку
                        e.Graphics.FillEllipse(Brushes.LimeGreen, f.Bounds.X + 10, f.Bounds.Y + 10, f.Bounds.Width - 20, f.Bounds.Height - 40);
                    }

                    // 2. Полоска прогресса (строго под цветком, внутри нижней части ячейки)
                    float ratio = f.WaterLevel / 100f;
                    int barW = f.Bounds.Width - 20; // Чуть уже чем ячейка
                    int barH = 8;
                    int barX = f.Bounds.X + 10;
                    int barY = f.Bounds.Y + f.Bounds.Height - 20; // В самом низу ячейки

                    // Фон полосы
                    e.Graphics.FillRectangle(Brushes.Gray, barX, barY, barW, barH);
                    // Заполнение
                    e.Graphics.FillRectangle(Brushes.Cyan, barX, barY, barW * ratio, barH);
                    // Рамка
                    e.Graphics.DrawRectangle(Pens.White, barX, barY, barW, barH);
                }

                // 3. Курсор-лейка при поливе
                if (isWatering)
                {
                    e.Graphics.DrawString("", new Font("Arial", 24), Brushes.White, wateringPos.X - 12, wateringPos.Y - 35);
                }

                return; // Не рисуем игру под оверлеем
            }

            // === МИНИ-ИГРА: РАДИО ===
            if (isRadioGameActive)
            {
                // Затемнение
                using (Brush overlay = new SolidBrush(Color.FromArgb(200, 20, 10, 30)))
                    e.Graphics.FillRectangle(overlay, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

                // Заголовок
                Font titleFont = new Font("Arial", 20, FontStyle.Bold);
                e.Graphics.DrawString("📻 Настрой радио на " + targetFreq.ToString("F1") + " МГц", titleFont, Brushes.LightYellow,
                    new PointF((this.ClientSize.Width - 420) / 2, radioBarBounds.Y - 60));

                // Панель частот
                e.Graphics.FillRectangle(Brushes.DarkGray, radioBarBounds);
                e.Graphics.DrawRectangle(Pens.Silver, radioBarBounds);

                // Зелёная зона цели
                float targetRatio = (targetFreq - 88.0f) / 20.0f;
                int zoneX = radioBarBounds.X + (int)(radioBarBounds.Width * targetRatio);
                int zoneW = 30; // Ширина допустимой зоны
                e.Graphics.FillRectangle(Brushes.LightGreen, zoneX - zoneW / 2, radioBarBounds.Y, zoneW, radioBarBounds.Height);

                // Ползунок
                float freqRatio = (radioFreq - 88.0f) / 20.0f;
                int needleX = radioBarBounds.X + (int)(radioBarBounds.Width * freqRatio);
                e.Graphics.FillRectangle(Brushes.Red, needleX - 3, radioBarBounds.Y - 10, 6, radioBarBounds.Height + 20);

                // Текущая частота
                Font freqFont = new Font("Arial", 16, FontStyle.Bold);
                e.Graphics.DrawString(radioFreq.ToString("F1") + " MHz", freqFont, Brushes.White,
                    new PointF(needleX - 25, radioBarBounds.Y - 35));

                // Подсказка
                e.Graphics.DrawString("Зажми ЛКМ и двигай мышь влево/вправо", new Font("Arial", 12), Brushes.Gray,
                    new PointF((this.ClientSize.Width - 320) / 2, radioBarBounds.Bottom + 20));

                return;
            }

            // 5. МИНИ-ИГРА (Если активна — рисуем поверх всего и выходим)
            if (isMiniGameActive)
            {
                using (Brush overlay = new SolidBrush(Color.FromArgb(220, 30, 30, 40)))
                    e.Graphics.FillRectangle(overlay, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

                Font hintFont = new Font("Arial", 20, FontStyle.Bold);
                string hintText = "Найди коробку с номером 18046";
                SizeF hintSize = e.Graphics.MeasureString(hintText, hintFont);
                e.Graphics.DrawString(hintText, hintFont, Brushes.Yellow,
                    new PointF((this.ClientSize.Width - hintSize.Width) / 2, 30));

                Font boxFont = new Font("Arial", 11, FontStyle.Bold);
                foreach (var box in mailOptions)
                {
                    if (boxSprite != null)
                        e.Graphics.DrawImage(boxSprite, box.Bounds);
                    else
                    {
                        e.Graphics.FillRectangle(Brushes.SaddleBrown, box.Bounds);
                        e.Graphics.DrawRectangle(Pens.Gold, box.Bounds);
                    }

                    SizeF textSize = e.Graphics.MeasureString(box.Number, boxFont);
                    PointF textPoint = new PointF(
                        box.Bounds.X + (box.Bounds.Width - textSize.Width) / 2,
                        box.Bounds.Y + (box.Bounds.Height - textSize.Height) / 2 + 25);
                    e.Graphics.DrawString(box.Number, boxFont, Brushes.White, textPoint);
                }
                return;
            }

            // 6. ДИАЛОГОВОЕ ОКНО
            // 6. ДИАЛОГОВОЕ ОКНО
            if (isDialogueActive)
            {
                using (Brush dimBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 30)))
                    e.Graphics.FillRectangle(dimBrush, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

                int panelH = 200;
                int panelW = this.ClientSize.Width - 120;
                int panelX = 60;
                int panelY = this.ClientSize.Height - panelH - 40;

                // Определяем, чья очередь говорить (чётный индекс = NPC, нечётный = Игрок)
                bool isPlayerTurn = (dialogueLineIndex % 2 != 0);
                string currentName = isPlayerTurn ? playerDisplayName : dialogueSpeaker;
                Bitmap? currentImg = isPlayerTurn ? playerPortrait : dialogueSprite;

                // Рисуем портрет
                if (currentImg != null)
                {
                    int targetH = 800;
                    int targetW = (int)(targetH * ((float)currentImg.Width / currentImg.Height));
                    int spriteX = panelX + 50;
                    int spriteY = panelY - targetH + 10;
                    e.Graphics.DrawImage(currentImg, spriteX, spriteY, targetW, targetH);
                }

                // Панель
                using (Brush panelBrush = new SolidBrush(Color.FromArgb(245, 235, 215)))
                using (Pen panelPen = new Pen(Color.FromArgb(120, 90, 60), 3))
                {
                    e.Graphics.FillRectangle(panelBrush, panelX, panelY, panelW, panelH);
                    e.Graphics.DrawRectangle(panelPen, panelX, panelY, panelW, panelH);
                }

                // Имя говорящего
                Font nameFont = new Font("Arial", 14, FontStyle.Bold);
                SizeF nameSize = e.Graphics.MeasureString(currentName, nameFont);
                int nameW = (int)nameSize.Width + 30;
                int nameH = 28;
                int nameX = panelX + 25;
                int nameY = panelY - 14;

                using (Brush nameBgBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                using (Pen namePen = new Pen(Color.FromArgb(120, 90, 60), 2))
                {
                    e.Graphics.FillRectangle(nameBgBrush, nameX, nameY, nameW, nameH);
                    e.Graphics.DrawRectangle(namePen, nameX, nameY, nameW, nameH);
                }
                e.Graphics.DrawString(currentName, nameFont, Brushes.Black, nameX + 15, nameY + 4);

                // Текст реплики
                string currentText = "";
                if (dialogueLines != null && dialogueLineIndex >= 0 && dialogueLineIndex < dialogueLines.Count)
                    currentText = dialogueLines[dialogueLineIndex];

                Font textFont = new Font("Comic Sans", 23, FontStyle.Regular);
                RectangleF textRect = new RectangleF(panelX + 30, panelY + 25, panelW - 60, panelH - 40);
                using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Near })
                    e.Graphics.DrawString(currentText, textFont, Brushes.Black, textRect, sf);

                Font arrowFont = new Font("Comic Sans", 12, FontStyle.Bold);
                e.Graphics.DrawString("▼ Нажми, чтобы продолжить", arrowFont, Brushes.Gray, panelX + panelW - 220, panelY + panelH - 30);

                return;
            }

            // 7. Общая подсказка внизу экрана
            e.Graphics.DrawString("Кликни на соседа для диалога",
                new Font("Arial", 9), Brushes.DarkGray, 10, 10);
        }

        private void StartStory()
        {
            currentGameState = Models.GameState.Quest1_Talk;
            SpawnNPC("Мила", 1400, 500, new List<string>
            {
                "Ох, привет! Давно не виделись, соседка! Как у тебя дела, все в порядке?",
                "Знаешь, по правде говоря, у меня произошла одна неприятность. Я гуляла во дворе, и, кажется, где-то потеряла свои ключи... Теперь я не могу вернуться домой!",
                "Что же мне теперь делать? Кажется, я уже везде их посмотрела. Если тебе не сложно, помоги мне в поисках! Они такие маленькие и блестящие. Возможно, они где-то недалеко..."
            }, "sprite1.png", 270, 270, "portrait1.png");

        }

        private void SpawnKeys()
        {
            if (collectibles.Exists(c => c.Item.Name == "Ключи" && !c.IsPickedUp))
                return;

            Item keyItem = new Item("Ключи", "Блестящие ключи от домика", Color.Gold);
            Collectible keys = new Collectible(310, 460, keyItem, "spritekey.png");
            collectibles.Add(keys);
            gameObjects.Add(keys);
            this.Invalidate();
            // ✅ УДАЛЕНА ОШИБОЧНАЯ СТРОКА С NPC
        }

        // ✅ Метод теперь принимает 7 параметров (с размерами)
        // Метод теперь принимает 8 параметров (добавлен portraitFile)
        private void SpawnNPC(string name, int x, int y, List<string> lines, string spriteName, int width, int height, string portraitFile = "")
        {
            NPC newNpc = new NPC(x, y, name, lines, spriteName, width, height, portraitFile);
            npcs.Add(newNpc);
            gameObjects.Add(newNpc);
            this.Invalidate();
        }

        private void StartQuest2()
        {
            var grandma = npcs.Find(n => n.DisplayName == "Мила");
            if (grandma != null)
            {
                gameObjects.Remove(grandma);
                npcs.Remove(grandma);
            }

            currentGameState = Models.GameState.Quest2_Spawn;
            SpawnNPC("Оливер", 600, 400, new List<string>
            {
                "Привет, соседка! Ты сегодня просто сияешь ярче солнышка! Я правда очень рад тебя видеть",
                "Слушай, мне неловко тебя просить, но... Не могла бы ты оказать мне одну услугу? Дело в том, что мне нужно срочно забрать посылку с почты. Но я сейчас очень занят, бегу по делам!",
                "Забери, пожалуйста, мой заказ с почтового пункта. Номер коробки - 18046. С меня шоколадка ха-ха!"
            }, "sprite2.png", 250, 250, "portrait2.png");
        }

        private void StartMailboxMiniGame()
        {
            mailOptions.Clear();
            Random rnd = new Random();
            int correctIndex = rnd.Next(0, 50);

            int cols = 10, rows = 5, boxSize = 100, gap = 20;
            int totalWidth = cols * (boxSize + gap) - gap;
            int totalHeight = rows * (boxSize + gap) - gap;
            int startX = (this.ClientSize.Width - totalWidth) / 2;
            int startY = (this.ClientSize.Height - totalHeight) / 2 + 30;

            for (int i = 0; i < 50; i++)
            {
                int row = i / cols, col = i % cols;
                int x = startX + col * (boxSize + gap);
                int y = startY + row * (boxSize + gap);
                string number = (i == correctIndex) ? "18046" : rnd.Next(10000, 99999).ToString();

                mailOptions.Add(new MailBoxOption
                {
                    Bounds = new Rectangle(x, y, boxSize, boxSize),
                    Number = number,
                    IsCorrect = (i == correctIndex)
                });
            }

            isMiniGameActive = true;
            currentGameState = Models.GameState.Quest2_MiniGame;
            this.Invalidate();
        }

        private void StartFlowerMiniGame()
        {
            flowers.Clear();

            // Настройки сетки: 5 колонок, 3 ряда
            int cols = 5;
            int rows = 3;
            int cellSize = 100; // Размер одной ячейки (включает отступы)

            // Расчет размеров всей сетки
            int totalW = cols * cellSize;
            int totalH = rows * cellSize;

            // Центрирование на экране
            int startX = (this.ClientSize.Width - totalW) / 2;
            int startY = (this.ClientSize.Height - totalH) / 2 - 50;

            for (int i = 0; i < 15; i++)
            {
                int r = i / cols;
                int c = i % cols;

                // Каждая ячейка 100x100
                flowers.Add(new FlowerData
                {
                    Bounds = new Rectangle(startX + c * cellSize, startY + r * cellSize, cellSize, cellSize)
                });
            }

            isFlowerGameActive = true;
            currentGameState = Models.GameState.Quest3_Watering;
            this.Invalidate();
        }

        private void StartQuest3()
        {
            var petya = npcs.Find(n => n.DisplayName == "Оливер");
            if (petya != null)
            {
                gameObjects.Remove(petya);
                npcs.Remove(petya);
            }

            currentGameState = Models.GameState.Quest3_Spawn;
            SpawnNPC("Мелисса", 150, 400, new List<string>
            {
                "Добрый денек, моя любимая соседка! Только посмотри, какие цветочки я сегодня посадила! Очень красивые, правда? Тебе нравится",
                "Я очень рада! Садоводство - это прекрасно, хоть и очень выматывает. Фух, так устала... Не могла бы ты мне помочь?",
                "Смотри, ничего сложного! Нужно просто полить каждый цветочек водой из лейки. Убедись, что воды достаточно! Я пока присяду и чуток отдохну"
            }, "sprite3.png", 160, 180, "portrait3.png"); // ✅ Добавлены размеры
        }

        // Старый вызов (без ответов игрока)
        private void StartDialogue(string speaker, List<string> npcLines, string spriteFileName)
            => StartDialogue(speaker, npcLines, new List<string>(), spriteFileName);

        // Новый вызов (с уникальными ответами игрока)
        private void StartDialogue(string speaker, List<string> npcLines, List<string> playerLines, string spriteFileName)
        {
            isDialogueActive = true;
            dialogueSpeaker = speaker;
            dialogueLineIndex = 0;

            // Объединяем реплики: NPC -> Игрок -> NPC -> Игрок...
            var combined = new List<string>();
            for (int i = 0; i < npcLines.Count; i++)
            {
                combined.Add(npcLines[i]);
                if (i < playerLines.Count) combined.Add(playerLines[i]);
            }
            dialogueLines = combined;

            // Загрузка портрета NPC
            NPC? n = npcs.Find(x => x.DisplayName == speaker);
            string pFile = n?.PortraitFileName ?? spriteFileName;
            try { dialogueSprite = new Bitmap($"Assets/{pFile}"); }
            catch { try { dialogueSprite = new Bitmap($"Assets/{spriteFileName}"); } catch { dialogueSprite = null; } }

            this.Invalidate();
        }

        private void AdvanceDialogue()
        {
            dialogueLineIndex++;

            if (dialogueLineIndex >= dialogueLines.Count)
            {
                isDialogueActive = false;
                dialogueSprite?.Dispose();
                dialogueSprite = null;
                this.Invalidate();

                if (currentGameState == Models.GameState.Quest1_Talk)
                {
                    currentGameState = Models.GameState.Quest1_Find;
                    SpawnKeys();
                    MessageBox.Show("Ищи ключи! Они где-то во дворе.", "Задание");
                }
                else if (currentGameState == Models.GameState.Quest1_Return)
                {
                    inventory.Remove("Ключи");
                    MessageBox.Show("Мила ушла домой. Появился Оливер!", "Квест выполнен");
                    StartQuest2();
                }
                else if (currentGameState == Models.GameState.Quest2_Spawn)
                {
                    MessageBox.Show("Найди на складе заказ 18046.", "Оливер");
                }
                else if (currentGameState == Models.GameState.Quest2_Deliver)
                {
                    inventory.Remove("Посылка №18046");
                    MessageBox.Show("Оливер ушёл. Появилась Мелисса!", "Квест выполнен");
                    StartQuest3();
                }
                else if (currentGameState == Models.GameState.Quest3_Completed)
                {
                    // Мелисса благодарит и исчезает, появляется Ричард
                    var melissa = npcs.Find(n => n.DisplayName == "Мелисса");
                    if (melissa != null) { gameObjects.Remove(melissa); npcs.Remove(melissa); }

                    currentGameState = Models.GameState.Quest4_Spawn;
                    gameObjects.Add(new Radio(800, 400));
                    SpawnNPC("Ричард", 950, 400, new List<string>
                {
                    "Ой, это ты! Спасибо, что пришла. Я помню что мы должны были сегодня слушать музыку, но у меня тут некая проблема с радио...",
                    "Ты видишь, оно совсем не хочет ловить нужную частоту. Ты случайно не разбираешься в радиотехнике?",
                    "О, класс, то что нужно! Помоги настроить его на 95.5 МГц! Я уверен, что ты справишься. Просто нажми на радио"
                }, "sprite4.png", 160, 180, "portrait4.png");
                    MessageBox.Show("Мелисса ушла. Ричард ждет помощи у баков!", "Задание обновлено");
                }

                else if (currentGameState == Models.GameState.Quest4_Spawn)
                {
                    currentGameState = Models.GameState.Quest4_Talk;
                    MessageBox.Show("Теперь кликни по радио на поле!", "Подсказка");
                }

                else if (currentGameState == Models.GameState.Quest4_Completed)
                {
                    MessageBox.Show("Поздравляем! Ты помог всем соседям!\nДвор стал самым уютным местом в городе!", "Победа!");
                    Application.Exit();
                }
                return;
            }
            this.Invalidate();
        }

        private void StartRadioMiniGame()
        {
            isRadioGameActive = true;
            radioFreq = 88.0f;
            targetFreq = 88.0f + (float)(new Random().NextDouble() * 15); // Случайная цель от 88 до 103
            currentGameState = Models.GameState.Quest4_Radio;

            // Границы панели (по центру экрана)
            radioBarBounds = new Rectangle(
                (this.ClientSize.Width - 400) / 2,
                this.ClientSize.Height / 2 - 20,
                400, 40
            );

            this.Invalidate();
        }

        public class MailBoxOption
        {
            public Rectangle Bounds { get; set; }
            public string Number { get; set; } = "";
            public bool IsCorrect { get; set; }
        }

        public class FlowerData
        {
            public Rectangle Bounds { get; set; }
            public int WaterLevel { get; set; } = 0; // 0..100
            public bool IsFull => WaterLevel >= 100;
        }
    }
}