namespace NeighborlyHelp.Models
{
    /// <summary>
    /// Перечисление состояний игры, определяющее текущий этап квеста.
    /// Используется GameController для управления потоком игры.
    /// </summary>
    public enum GameState
    {
        Intro,
        Quest1_Talk,
        Quest1_Find,
        Quest1_Return,
        Quest2_Spawn,
        Quest2_MiniGame,
        Quest2_Deliver,
        Quest3_Spawn,
        Quest3_Talk,
        Quest3_Watering,
        Quest3_Completed,
        Quest4_Spawn,     // Появление Ричарда
        Quest4_Talk,      // Диалог с Ричардом
        Quest4_Radio,     // Мини-игра с радио
        Quest4_Completed
    }
}