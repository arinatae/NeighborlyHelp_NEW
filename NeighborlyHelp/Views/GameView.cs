using NeighborlyHelp.Views;
using System.Windows.Forms;



using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NeighborlyHelp.Views
{
    /// <summary>
    /// View-компонент для отрисовки игры (View в MVC).
    /// Отвечает за рендеринг всех игровых объектов, UI, мини-игр и диалогов.
    /// Не содержит игровой логики — только отрисовка.
    /// </summary>
    public class GameView
    {
        private readonly Form _form;
        private Bitmap? _playerSprite;
        private Bitmap? _backgroundImage;
        private Bitmap? _boxSprite;
        private Bitmap? _flowerSprite;
        private Bitmap? _playerPortrait;

        /// <summary>
        /// Конструктор GameView. Загружает спрайты и настраивает форму.
        /// </summary>
        public GameView(Form form)
        {
            _form = form;
            LoadSprites();
            SetupForm();
        }

        /// <summary>
        /// Загружает все спрайты из папки Assets.
        /// </summary>
        private void LoadSprites()
        {
            try { _playerSprite = new Bitmap("Assets/sprite0.png"); }
            catch { _playerSprite = null; }

            try
            {
                _backgroundImage = new Bitmap("Assets/backpicture.png");
            }
            catch { _backgroundImage = null; }

            try { _boxSprite = new Bitmap("Assets/sprite-box.png"); }
            catch { _boxSprite = null; }

            try { _flowerSprite = new Bitmap("Assets/spriteflower.png"); }
            catch { _flowerSprite = null; }

            try { _playerPortrait = new Bitmap("Assets/portrait0.png"); }
            catch { _playerPortrait = null; }
        }

        /// <summary>
        /// Настраивает форму для полноэкранного режима и двойной буферизации.
        /// </summary>
        private void SetupForm()
        {
            _form.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.UserPaint, true);
            _form.DoubleBuffered = true;
            _form.Text = "🏡 Соседская помощь";
            _form.FormBorderStyle = FormBorderStyle.None;
            _form.WindowState = FormWindowState.Normal;
            _form.Size = Screen.PrimaryScreen.Bounds.Size;
            _form.Location = new Point(0, 0);
            _form.StartPosition = FormStartPosition.Manual;
            _form.BackColor = ColorTranslator.FromHtml("#87CEEB");
            _form.KeyPreview = true;
        }

        /// <summary>
        /// Обновляет фон при изменении размера окна.
        /// </summary>
        public void ResizeBackground(int width, int height)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.Dispose();
            }
            try
            {
                _backgroundImage = new Bitmap("Assets/backpicture.png");
                _backgroundImage = new Bitmap(_backgroundImage, width, height);
            }
            catch { _backgroundImage = null; }
        }

        /// <summary>
        /// Основной метод отрисовки. Вызывается из OnPaint формы.
        /// </summary>
        public void Render(Graphics g, Models.GameModel model, DialogState? dialogState)
        {
            // 1. Фон
            if (_backgroundImage != null)
                g.DrawImage(_backgroundImage, 0, 0, model.GameField.Width, model.GameField.Height);
            else
                g.Clear(_form.BackColor);

            // 2. Объекты (деревья, скамейки, стены)
            foreach (var obj in model.GameObjects)
                obj.Draw(g);

            // 3. Игрок
            if (_playerSprite != null)
                g.DrawImage(_playerSprite, model.Player.X, model.Player.Y, model.Player.Width, model.Player.Height);

            // 4. Подсказка взаимодействия
            if (!string.IsNullOrEmpty(model.InteractionHint))
            {
                DrawInteractionHint(g, model);
            }

            // 5. Мини-игра: Полив цветов
            if (model.IsFlowerGameActive)
            {
                RenderFlowerMiniGame(g, model);
                return;
            }

            // 6. Мини-игра: Радио
            if (model.IsRadioGameActive)
            {
                RenderRadioMiniGame(g, model);
                return;
            }

            // 7. Мини-игра: Почтовые ящики
            if (model.IsMiniGameActive)
            {
                RenderMailboxMiniGame(g, model);
                return;
            }

            // 8. Диалоговое окно
            if (dialogState != null && dialogState.IsActive)
            {
                RenderDialogue(g, model, dialogState);
                return;
            }

            // 9. Общая подсказка внизу экрана
            g.DrawString("Кликни на соседа для диалога",
                new Font("Arial", 9), Brushes.DarkGray, 10, 10);
        }

        /// <summary>
        /// Рисует подсказку взаимодействия над игроком.
        /// </summary>
        private void DrawInteractionHint(Graphics g, Models.GameModel model)
        {
            Font hintFont = new Font("Arial", 14, FontStyle.Bold);
            SizeF hintSize = g.MeasureString(model.InteractionHint, hintFont);

            float x = model.Player.X + model.Player.Width / 2 - hintSize.Width / 2;
            float y = model.Player.Y - 30;

            using (Brush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(bgBrush, x - 5, y - 2, hintSize.Width + 10, hintSize.Height + 4);
            }

            g.DrawString(model.InteractionHint, hintFont, Brushes.White, x, y);
        }

        /// <summary>
        /// Отрисовка мини-игры с цветами.
        /// </summary>
        private void RenderFlowerMiniGame(Graphics g, Models.GameModel model)
        {
            using (Brush overlay = new SolidBrush(Color.FromArgb(210, 10, 30, 10)))
                g.FillRectangle(overlay, 0, 0, _form.ClientSize.Width, _form.ClientSize.Height);

            Font titleFont = new Font("Arial", 20, FontStyle.Bold);
            g.DrawString("🌿 Полей все цветы из лейки", titleFont, Brushes.LightGreen,
                new PointF((_form.ClientSize.Width - 380) / 2, 40));

            foreach (var f in model.Flowers)
            {
                if (_flowerSprite != null)
                {
                    int drawW = f.Bounds.Width - 20;
                    int drawH = f.Bounds.Height - 40;
                    int drawX = f.Bounds.X + 10;
                    int drawY = f.Bounds.Y + 10;
                    g.DrawImage(_flowerSprite, drawX, drawY, drawW, drawH);
                }
                else
                {
                    g.FillEllipse(Brushes.LimeGreen, f.Bounds.X + 10, f.Bounds.Y + 10, f.Bounds.Width - 20, f.Bounds.Height - 40);
                }

                float ratio = f.WaterLevel / 100f;
                int barW = f.Bounds.Width - 20;
                int barH = 8;
                int barX = f.Bounds.X + 10;
                int barY = f.Bounds.Y + f.Bounds.Height - 20;

                g.FillRectangle(Brushes.Gray, barX, barY, barW, barH);
                g.FillRectangle(Brushes.Cyan, barX, barY, (int)(barW * ratio), barH);
                g.DrawRectangle(Pens.White, barX, barY, barW, barH);
            }

            if (model.IsWatering)
            {
                g.DrawString("", new Font("Arial", 24), Brushes.White, model.WateringPos.X - 12, model.WateringPos.Y - 35);
            }
        }

        /// <summary>
        /// Отрисовка мини-игры с радио.
        /// </summary>
        private void RenderRadioMiniGame(Graphics g, Models.GameModel model)
        {
            using (Brush overlay = new SolidBrush(Color.FromArgb(200, 20, 10, 30)))
                g.FillRectangle(overlay, 0, 0, _form.ClientSize.Width, _form.ClientSize.Height);

            Font titleFont = new Font("Arial", 20, FontStyle.Bold);
            g.DrawString($"📻 Настрой радио на {model.TargetFreq:F1} МГц", titleFont, Brushes.LightYellow,
                new PointF((_form.ClientSize.Width - 420) / 2, model.RadioBarBounds.Y - 60));

            g.FillRectangle(Brushes.DarkGray, model.RadioBarBounds);
            g.DrawRectangle(Pens.Silver, model.RadioBarBounds);

            float targetRatio = (model.TargetFreq - 88.0f) / 20.0f;
            int zoneX = model.RadioBarBounds.X + (int)(model.RadioBarBounds.Width * targetRatio);
            int zoneW = 30;
            g.FillRectangle(Brushes.LightGreen, zoneX - zoneW / 2, model.RadioBarBounds.Y, zoneW, model.RadioBarBounds.Height);

            float freqRatio = (model.RadioFreq - 88.0f) / 20.0f;
            int needleX = model.RadioBarBounds.X + (int)(model.RadioBarBounds.Width * freqRatio);
            g.FillRectangle(Brushes.Red, needleX - 3, model.RadioBarBounds.Y - 10, 6, model.RadioBarBounds.Height + 20);

            Font freqFont = new Font("Arial", 16, FontStyle.Bold);
            g.DrawString($"{model.RadioFreq:F1} MHz", freqFont, Brushes.White,
                new PointF(needleX - 25, model.RadioBarBounds.Y - 35));

            g.DrawString("Зажми ЛКМ и двигай мышь влево/вправо", new Font("Arial", 12), Brushes.Gray,
                new PointF((_form.ClientSize.Width - 320) / 2, model.RadioBarBounds.Bottom + 20));
        }

        /// <summary>
        /// Отрисовка мини-игры с почтовыми ящиками.
        /// </summary>
        private void RenderMailboxMiniGame(Graphics g, Models.GameModel model)
        {
            using (Brush overlay = new SolidBrush(Color.FromArgb(220, 30, 30, 40)))
                g.FillRectangle(overlay, 0, 0, _form.ClientSize.Width, _form.ClientSize.Height);

            Font hintFont = new Font("Arial", 20, FontStyle.Bold);
            string hintText = "Найди коробку с номером 18046";
            SizeF hintSize = g.MeasureString(hintText, hintFont);
            g.DrawString(hintText, hintFont, Brushes.Yellow,
                new PointF((_form.ClientSize.Width - hintSize.Width) / 2, 30));

            Font boxFont = new Font("Arial", 11, FontStyle.Bold);
            foreach (var box in model.MailOptions)
            {
                if (_boxSprite != null)
                    g.DrawImage(_boxSprite, box.Bounds);
                else
                {
                    g.FillRectangle(Brushes.SaddleBrown, box.Bounds);
                    g.DrawRectangle(Pens.Gold, box.Bounds);
                }

                SizeF textSize = g.MeasureString(box.Number, boxFont);
                PointF textPoint = new PointF(
                    box.Bounds.X + (box.Bounds.Width - textSize.Width) / 2,
                    box.Bounds.Y + (box.Bounds.Height - textSize.Height) / 2 + 25);
                g.DrawString(box.Number, boxFont, Brushes.White, textPoint);
            }
        }

        /// <summary>
        /// Отрисовка диалогового окна.
        /// </summary>
        private void RenderDialogue(Graphics g, Models.GameModel model, DialogState dialogState)
        {
            using (Brush dimBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 30)))
                g.FillRectangle(dimBrush, 0, 0, _form.ClientSize.Width, _form.ClientSize.Height);

            int panelH = 200;
            int panelW = _form.ClientSize.Width - 120;
            int panelX = 60;
            int panelY = _form.ClientSize.Height - panelH - 40;

            bool isPlayerTurn = (dialogState.LineIndex % 2 != 0);
            string currentName = isPlayerTurn ? "Ты" : dialogState.Speaker;
            Bitmap? currentImg = isPlayerTurn ? _playerPortrait : dialogState.Sprite;

            if (currentImg != null)
            {
                int targetH = 800;
                int targetW = (int)(targetH * ((float)currentImg.Width / currentImg.Height));
                int spriteX = panelX + 50;
                int spriteY = panelY - targetH + 10;
                g.DrawImage(currentImg, spriteX, spriteY, targetW, targetH);
            }

            using (Brush panelBrush = new SolidBrush(Color.FromArgb(245, 235, 215)))
            using (Pen panelPen = new Pen(Color.FromArgb(120, 90, 60), 3))
            {
                g.FillRectangle(panelBrush, panelX, panelY, panelW, panelH);
                g.DrawRectangle(panelPen, panelX, panelY, panelW, panelH);
            }

            Font nameFont = new Font("Arial", 14, FontStyle.Bold);
            SizeF nameSize = g.MeasureString(currentName, nameFont);
            int nameW = (int)nameSize.Width + 30;
            int nameH = 28;
            int nameX = panelX + 25;
            int nameY = panelY - 14;

            using (Brush nameBgBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (Pen namePen = new Pen(Color.FromArgb(120, 90, 60), 2))
            {
                g.FillRectangle(nameBgBrush, nameX, nameY, nameW, nameH);
                g.DrawRectangle(namePen, nameX, nameY, nameW, nameH);
            }
            g.DrawString(currentName, nameFont, Brushes.Black, nameX + 15, nameY + 4);

            string currentText = "";
            if (dialogState.Lines != null && dialogState.LineIndex >= 0 && dialogState.LineIndex < dialogState.Lines.Count)
                currentText = dialogState.Lines[dialogState.LineIndex];

            Font textFont = new Font("Comic Sans", 23, FontStyle.Regular);
            RectangleF textRect = new RectangleF(panelX + 30, panelY + 25, panelW - 60, panelH - 40);
            using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Near })
                g.DrawString(currentText, textFont, Brushes.Black, textRect, sf);

            Font arrowFont = new Font("Comic Sans", 12, FontStyle.Bold);
            g.DrawString("▼ Нажми, чтобы продолжить", arrowFont, Brushes.Gray, panelX + panelW - 220, panelY + panelH - 30);
        }

        /// <summary>
        /// Получает спрайт портрета для NPC.
        /// </summary>
        public Bitmap? GetPortrait(string spriteFileName)
        {
            try { return new Bitmap($"Assets/{spriteFileName}"); }
            catch { return null; }
        }

        /// <summary>
        /// Освобождает ресурсы.
        /// </summary>
        public void Dispose()
        {
            _playerSprite?.Dispose();
            _backgroundImage?.Dispose();
            _boxSprite?.Dispose();
            _flowerSprite?.Dispose();
            _playerPortrait?.Dispose();
        }
    }

    /// <summary>
    /// Состояние диалога для передачи в View.
    /// </summary>
    public class DialogState
    {
        public bool IsActive { get; set; }
        public string Speaker { get; set; } = "";
        public List<string> Lines { get; set; } = new List<string>();
        public int LineIndex { get; set; }
        public Bitmap? Sprite { get; set; }
    }
}