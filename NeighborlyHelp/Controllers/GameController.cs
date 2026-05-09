using NeighborlyHelp.Controllers;
using NeighborlyHelp.Models;



using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NeighborlyHelp.Models;
using NeighborlyHelp.Views;

namespace NeighborlyHelp.Controllers
{
    /// <summary>
    /// Controller в MVC-архитектуре.
    /// Обрабатывает ввод пользователя, управляет игровым циклом и связывает Model с View.
    /// </summary>
    public class GameController
    {
        private readonly GameModel _model;
        private readonly GameView _view;
        private readonly Form _form;
        private Timer _gameTimer = null!;
        private Timer _hintTimer = null!;
        private DialogState _dialogState = new DialogState();

        // === Спрайты для диалогов ===
        private Bitmap? _dialogueSprite;

        // === Подсказка взаимодействия ===
        private string _interactionHint = "";

        /// <summary>
        /// Конструктор GameController.
        /// </summary>
        public GameController(Form form, GameModel model, GameView view)
        {
            _form = form;
            _model = model;
            _view = view;
        }

        /// <summary>
        /// Инициализирует контроллер: настраивает таймеры и обработчики событий.
        /// </summary>
        public void Initialize()
        {
            _model.Initialize();

            // Настройка игрового таймера
            _gameTimer = new Timer { Interval = 16 };
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            // Настройка таймера подсказок
            _hintTimer = new Timer { Interval = 2000 };
            _hintTimer.Tick += HintTimer_Tick;

            // Подписка на события формы
            _form.KeyDown += Form_KeyDown;
            _form.MouseClick += Form_MouseClick;
            _form.MouseDown += Form_MouseDown;
            _form.MouseUp += Form_MouseUp;
            _form.MouseMove += Form_MouseMove;
            _form.Resize += Form_Resize;
            _form.Paint += Form_Paint;

            // Запуск сюжета
            StartStory();
        }

        /// <summary>
        /// Обработчик изменения размера окна.
        /// </summary>
        private void Form_Resize(object? sender, EventArgs e)
        {
            _model.ResizeField(_form.ClientSize.Width, _form.ClientSize.Height);
            _view.ResizeBackground(_form.ClientSize.Width, _form.ClientSize.Height);
            _form.Invalidate();
        }

        /// <summary>
        /// Обработчик отрисовки.
        /// </summary>
        private void Form_Paint(object? sender, PaintEventArgs e)
        {
            _view.Render(e.Graphics, _model, _dialogState.IsActive ? _dialogState : null);
        }

        /// <summary>
        /// Основной игровой цикл (обновление логики).
        /// </summary>
        private void GameLoop(object? sender, EventArgs e)
        {
            // Полив цветов
            _model.WaterFlowers();

            // Проверка победы в мини-игре с цветами
            if (_model.CheckFlowerGameWin())
            {
                MessageBox.Show("🌸 Все цветы расцвели! Отличная работа!", "Успех");
            }

            // Проверка победы в мини-игре с радио
            if (_model.CheckRadioGameWin())
            {
                MessageBox.Show($"📻 Частота {_model.RadioFreq:F1} МГц поймана! Передача идет!", "Успех");
            }

            _form.Invalidate();
        }

        /// <summary>
        /// Обработчик нажатия клавиш.
        /// </summary>
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            int newX = _model.Player.X;
            int newY = _model.Player.Y;
            int speed = _model.Player.Speed;

            switch (e.KeyCode)
            {
                case Keys.W: case Keys.Up: newY -= speed; break;
                case Keys.S: case Keys.Down: newY += speed; break;
                case Keys.A: case Keys.Left: newX -= speed; break;
                case Keys.D: case Keys.Right: newX += speed; break;

                case Keys.Escape:
                    HandlePause();
                    return;

                case Keys.I:
                    MessageBox.Show($"🎒 Инвентарь:\n{_model.Inventory.GetList()}", "Инвентарь");
                    return;
            }

            _model.TryMovePlayer(newX, newY);
            _form.Invalidate();
        }

        /// <summary>
        /// Обработка паузы игры.
        /// </summary>
        private void HandlePause()
        {
            _gameTimer.Stop();
            var result = MessageBox.Show(
                "Игра на паузе!\n\nВыберите действие:",
                "Пауза",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes) _gameTimer.Start();
            else if (result == DialogResult.No) Application.Exit();
            else _gameTimer.Start();
        }

        /// <summary>
        /// Обработчик клика мыши.
        /// </summary>
        private void Form_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_dialogState.IsActive)
            {
                AdvanceDialogue();
                return;
            }

            if (_model.IsMiniGameActive)
            {
                HandleMailboxClick(e);
                return;
            }

            // Клик по почтовому ящику
            if (_model.CurrentState == GameState.Quest2_Spawn)
            {
                foreach (var obj in _model.GameObjects)
                {
                    if (obj is Mailbox && obj.Bounds.Contains(e.X, e.Y))
                    {
                        _model.StartMailboxMiniGame(_form.ClientSize.Width, _form.ClientSize.Height);
                        return;
                    }
                }
            }

            // Клик по клумбе
            if (_model.CurrentState == GameState.Quest3_Spawn)
            {
                foreach (var obj in _model.GameObjects)
                {
                    if (obj is FlowerBed && obj.Bounds.Contains(e.X, e.Y))
                    {
                        _model.StartFlowerMiniGame(_form.ClientSize.Width, _form.ClientSize.Height);
                        return;
                    }
                }
            }

            // Клик по радио
            if (_model.CurrentState == GameState.Quest4_Talk)
            {
                foreach (var obj in _model.GameObjects)
                {
                    if (obj is Radio && obj.Bounds.Contains(e.X, e.Y))
                    {
                        _model.StartRadioMiniGame(_form.ClientSize.Width, _form.ClientSize.Height);
                        return;
                    }
                }
            }

            // Клик по подбираемому предмету
            HandleCollectibleClick(e);

            // Клик по NPC
            HandleNPCClick(e);
        }

        /// <summary>
        /// Обработка клика по подбираемым предметам.
        /// </summary>
        private void HandleCollectibleClick(MouseEventArgs e)
        {
            foreach (var item in _model.Collectibles)
            {
                if (!item.IsPickedUp && item.Bounds.Contains(e.X, e.Y))
                {
                    if (!_model.IsCloseTo(item.Bounds))
                    {
                        ShowInteractionHint("Подойдите ближе!");
                        return;
                    }

                    item.IsPickedUp = true;
                    _model.Inventory.Add(item.Item);
                    ClearInteractionHint();

                    if (_model.CurrentState == GameState.Quest1_Find && item.Item.Name == "Ключи")
                    {
                        MessageBox.Show("Нашёл ключи! Отнеси их Миле.", "Находка");
                        _model.CurrentState = GameState.Quest1_Return;
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Обработка клика по NPC.
        /// </summary>
        private void HandleNPCClick(MouseEventArgs e)
        {
            foreach (var npc in _model.NPCs)
            {
                if (npc.IsDialogAvailable && npc.Bounds.Contains(e.X, e.Y))
                {
                    if (!_model.IsCloseTo(npc.Bounds))
                    {
                        ShowInteractionHint("Подойдите ближе!");
                        return;
                    }

                    List<string> linesToSay = npc.DialogLines;
                    List<string> playerLines = new List<string>();
                    string spriteName = "sprite1.png";

                    GetDialogueContent(npc, ref linesToSay, ref playerLines, ref spriteName);
                    StartDialogue(npc.DisplayName, linesToSay, playerLines, spriteName);
                    return;
                }
            }
        }

        /// <summary>
        /// Получает содержание диалога в зависимости от NPC и состояния игры.
        /// </summary>
        private void GetDialogueContent(NPC npc, ref List<string> linesToSay, ref List<string> playerLines, ref string spriteName)
        {
            if (npc.DisplayName == "Мила")
            {
                spriteName = "sprite1.png";
                if (_model.CurrentState == GameState.Quest1_Return)
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
                else
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
                if (_model.CurrentState == GameState.Quest2_Deliver)
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
                if (_model.CurrentState == GameState.Quest3_Completed)
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
                if (_model.CurrentState == GameState.Quest4_Spawn)
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
                else if (_model.CurrentState == GameState.Quest4_Completed)
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
        }

        /// <summary>
        /// Обработка клика по почтовым ящикам в мини-игре.
        /// </summary>
        private void HandleMailboxClick(MouseEventArgs e)
        {
            foreach (var box in _model.MailOptions)
            {
                if (box.Bounds.Contains(e.X, e.Y))
                {
                    if (box.IsCorrect)
                    {
                        MessageBox.Show("Посылка №18046 найдена! Отнеси её Оливеру.", "Успех");
                        _model.Inventory.Add(new Item("Посылка №18046", "Тяжелая коробка", Color.Brown));
                        _model.IsMiniGameActive = false;
                        _model.MailOptions.Clear();
                        _model.CurrentState = GameState.Quest2_Deliver;
                        _form.Invalidate();
                    }
                    else
                    {
                        MessageBox.Show("Не та коробка! Ищи посылку №18046.", "Ошибка");
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки мыши.
        /// </summary>
        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_model.IsFlowerGameActive)
            {
                _model.IsWatering = true;
                _model.WateringPos = e.Location;
            }

            if (_model.IsRadioGameActive && _model.RadioBarBounds.Contains(e.Location))
            {
                _model.IsDraggingRadio = true;
                _model.UpdateRadioFreq(e.X);
            }
        }

        /// <summary>
        /// Обработчик движения мыши.
        /// </summary>
        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_model.IsFlowerGameActive) _model.WateringPos = e.Location;

            if (_model.IsDraggingRadio)
            {
                _model.UpdateRadioFreq(e.X);
            }
        }

        /// <summary>
        /// Обработчик отпускания кнопки мыши.
        /// </summary>
        private void Form_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_model.IsFlowerGameActive) _model.IsWatering = false;
            _model.IsDraggingRadio = false;
        }

        /// <summary>
        /// Таймер скрытия подсказки.
        /// </summary>
        private void HintTimer_Tick(object? sender, EventArgs e)
        {
            ClearInteractionHint();
        }

        /// <summary>
        /// Показывает подсказку взаимодействия.
        /// </summary>
        private void ShowInteractionHint(string text)
        {
            _interactionHint = text;
            _model.InteractionHint = text;
            _hintTimer.Stop();
            _hintTimer.Start();
            _form.Invalidate();
        }

        /// <summary>
        /// Очищает подсказку взаимодействия.
        /// </summary>
        private void ClearInteractionHint()
        {
            _interactionHint = "";
            _model.InteractionHint = "";
            _form.Invalidate();
        }

        /// <summary>
        /// Запускает диалог с NPC.
        /// </summary>
        private void StartDialogue(string speaker, List<string> npcLines, List<string> playerLines, string spriteFileName)
        {
            _dialogState.IsActive = true;
            _dialogState.Speaker = speaker;
            _dialogState.LineIndex = 0;

            var combined = new List<string>();
            for (int i = 0; i < npcLines.Count; i++)
            {
                combined.Add(npcLines[i]);
                if (i < playerLines.Count) combined.Add(playerLines[i]);
            }
            _dialogState.Lines = combined;

            NPC? n = _model.NPCs.Find(x => x.DisplayName == speaker);
            string pFile = n?.PortraitFileName ?? spriteFileName;
            try { _dialogueSprite = new Bitmap($"Assets/{pFile}"); }
            catch { try { _dialogueSprite = new Bitmap($"Assets/{spriteFileName}"); } catch { _dialogueSprite = null; } }
            _dialogState.Sprite = _dialogueSprite;

            _form.Invalidate();
        }

        /// <summary>
        /// Переход к следующей реплике диалога.
        /// </summary>
        private void AdvanceDialogue()
        {
            _dialogState.LineIndex++;

            if (_dialogState.LineIndex >= _dialogState.Lines.Count)
            {
                EndDialogue();
                return;
            }
            _form.Invalidate();
        }

        /// <summary>
        /// Завершает диалог и обновляет состояние игры.
        /// </summary>
        private void EndDialogue()
        {
            _dialogState.IsActive = false;
            _dialogueSprite?.Dispose();
            _dialogueSprite = null;
            _form.Invalidate();

            HandleQuestProgression();
        }

        /// <summary>
        /// Обработка прогресса квестов после завершения диалога.
        /// </summary>
        private void HandleQuestProgression()
        {
            if (_model.CurrentState == GameState.Quest1_Talk)
            {
                _model.CurrentState = GameState.Quest1_Find;
                _model.SpawnKeys();
                MessageBox.Show("Ищи ключи! Они где-то во дворе.", "Задание");
            }
            else if (_model.CurrentState == GameState.Quest1_Return)
            {
                _model.Inventory.Remove("Ключи");
                MessageBox.Show("Мила ушла домой. Появился Оливер!", "Квест выполнен");
                StartQuest2();
            }
            else if (_model.CurrentState == GameState.Quest2_Spawn)
            {
                MessageBox.Show("Найди на складе заказ 18046.", "Оливер");
            }
            else if (_model.CurrentState == GameState.Quest2_Deliver)
            {
                _model.Inventory.Remove("Посылка №18046");
                MessageBox.Show("Оливер ушёл. Появилась Мелисса!", "Квест выполнен");
                StartQuest3();
            }
            else if (_model.CurrentState == GameState.Quest3_Completed)
            {
                _model.RemoveNPC("Мелисса");
                _model.CurrentState = GameState.Quest4_Spawn;
                _model.GameObjects.Add(new Radio(800, 400));
                _model.SpawnNPC("Ричард", 950, 400, new List<string>
                {
                    "Ой, это ты! Спасибо, что пришла. Я помню что мы должны были сегодня слушать музыку, но у меня тут некая проблема с радио...",
                    "Ты видишь, оно совсем не хочет ловить нужную частоту. Ты случайно не разбираешься в радиотехнике?",
                    "О, класс, то что нужно! Помоги настроить его на 95.5 МГц! Я уверен, что ты справишься. Просто нажми на радио"
                }, "sprite4.png", 160, 180, "portrait4.png");
                MessageBox.Show("Мелисса ушла. Ричард ждет помощи у баков!", "Задание обновлено");
            }
            else if (_model.CurrentState == GameState.Quest4_Spawn)
            {
                _model.CurrentState = GameState.Quest4_Talk;
                MessageBox.Show("Теперь кликни по радио на поле!", "Подсказка");
            }
            else if (_model.CurrentState == GameState.Quest4_Completed)
            {
                MessageBox.Show("Поздравляем! Ты помог всем соседям!\nДвор стал самым уютным местом в городе!", "Победа!");
                Application.Exit();
            }
        }

        /// <summary>
        /// Запускает второй квест (Оливер).
        /// </summary>
        private void StartQuest2()
        {
            _model.RemoveNPC("Мила");
            _model.CurrentState = GameState.Quest2_Spawn;
            _model.SpawnNPC("Оливер", 600, 400, new List<string>
            {
                "Привет, соседка! Ты сегодня просто сияешь ярче солнышка! Я правда очень рад тебя видеть",
                "Слушай, мне неловко тебя просить, но... Не могла бы ты оказать мне одну услугу? Дело в том, что мне нужно срочно забрать посылку с почты. Но я сейчас очень занят, бегу по делам!",
                "Забери, пожалуйста, мой заказ с почтового пункта. Номер коробки - 18046. С меня шоколадка ха-ха!"
            }, "sprite2.png", 250, 250, "portrait2.png");
        }

        /// <summary>
        /// Запускает третий квест (Мелисса).
        /// </summary>
        private void StartQuest3()
        {
            _model.RemoveNPC("Оливер");
            _model.CurrentState = GameState.Quest3_Spawn;
            _model.SpawnNPC("Мелисса", 150, 400, new List<string>
            {
                "Добрый денек, моя любимая соседка! Только посмотри, какие цветочки я сегодня посадила! Очень красивые, правда? Тебе нравится",
                "Я очень рада! Садоводство - это прекрасно, хоть и очень выматывает. Фух, так устала... Не могла бы ты мне помочь?",
                "Смотри, ничего сложного! Нужно просто полить каждый цветочек водой из лейки. Убедись, что воды достаточно! Я пока присяду и чуток отдохну"
            }, "sprite3.png", 160, 180, "portrait3.png");
        }

        /// <summary>
        /// Запускает начальный сюжет.
        /// </summary>
        private void StartStory()
        {
            _model.CurrentState = GameState.Quest1_Talk;
            _model.SpawnNPC("Мила", 1400, 500, new List<string>
            {
                "Ох, привет! Давно не виделись, соседка! Как у тебя дела, все в порядке?",
                "Знаешь, по правде говоря, у меня произошла одна неприятность. Я гуляла во дворе, и, кажется, где-то потеряла свои ключи... Теперь я не могу вернуться домой!",
                "Что же мне теперь делать? Кажется, я уже везде их посмотрела. Если тебе не сложно, помоги мне в поисках! Они такие маленькие и блестящие. Возможно, они где-то недалеко..."
            }, "sprite1.png", 270, 270, "portrait1.png");
        }
    }
}