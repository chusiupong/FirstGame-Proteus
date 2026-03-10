using System;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Action types for fitness game
    /// </summary>
    [Serializable]
    public enum ActionType
    {
        None = 0,
        BowDraw = 1,      // 拉弓射箭
        FacePull = 2      // Face Pull
    }
}
