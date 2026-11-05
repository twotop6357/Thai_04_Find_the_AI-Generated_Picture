using Core;
using VContainer;
using VContainer.Unity;
using HuliacDev.App;

namespace App
{
    /// <summary>
    /// Project-specific root DI scope.
    /// Adds scene manager injection on top of the logging, MessagePipe and settings registrations of the template RootLifetimeScope.
    /// </summary>
    public class GameLifetimeScope : RootLifetimeScope
    {
        /// <summary>
        /// Adds the project manager registrations to the template defaults (logging, MessagePipe, AppSettingsProvider).
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            ConfigureManagers(builder);
        }

        /// <summary>
        /// Registers manager components placed in the hierarchy with the container so method injection works.
        /// </summary>
        private void ConfigureManagers(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GameManager>();
        }
    }
}
