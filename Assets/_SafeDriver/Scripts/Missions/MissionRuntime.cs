using System;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Instancia runtime de una mision: trackea estado y progreso durante el juego.
    /// La crea la MissionDefinition (CreateRuntime) y el MissionManager maneja su
    /// ciclo de vida (Activate al cargar el nivel, Deactivate al terminar, Tick por frame).
    ///
    /// Se suscribe a lo que necesite (tipicamente EventBus.OnCorrectActionPerformed) en
    /// Activate() y limpia en Deactivate().
    /// </summary>
    public abstract class MissionRuntime
    {
        /// <summary>Definicion (data) de la que se origino esta instancia.</summary>
        public MissionDefinition Definition { get; }

        /// <summary>Estado actual de la mision.</summary>
        public MissionStatus Status { get; protected set; } = MissionStatus.Pending;

        /// <summary>Progreso normalizado 0..1 para barras/iconos de UI.</summary>
        public abstract float Progress01 { get; }

        /// <summary>Texto de progreso para mostrar (ej "1/3", "12s"), o vacio si no aplica.</summary>
        public virtual string ProgressLabel => string.Empty;

        /// <summary>Se dispara cuando cambia el progreso o el estado.</summary>
        public event Action<MissionRuntime> Changed;

        protected MissionRuntime(MissionDefinition definition)
        {
            Definition = definition;
        }

        /// <summary>Suscribe a eventos y arranca tracking. Llamado por MissionManager al cargar el nivel.</summary>
        public abstract void Activate();

        /// <summary>Desuscribe. Llamado por MissionManager al terminar el nivel.</summary>
        public abstract void Deactivate();

        /// <summary>Avanza timers internos si la mision los usa. Llamado por MissionManager cada frame.</summary>
        public virtual void Tick(float deltaTime) { }

        protected void RaiseChanged() => Changed?.Invoke(this);
    }
}
