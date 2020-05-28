using System;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Localization;

//[assembly: ShinyLocalizationAutoRegisterAttribute]

namespace Shiny
{
    //public class ShinyLocalizationAutoRegisterAttribute : AutoRegisterAttribute
    //{
    //    public override void Register(IServiceCollection services)
    //    {
    //        var textProviderImplType = this.FindImplementationType(typeof(ITextProvider), false);
    //        services.UseLocalization(textProviderImplType, );
    //    }
    //}


    public class ShinyLocalizationAttribute : ServiceModuleAttribute
    {
        public ShinyLocalizationAttribute(Type textProviderType, bool autoInitialize = true, bool tryParents = true, bool refreshAvailableCultures = true, string? initializationCultureName = null, string? defaultInvariantCultureName = null) : 
            this(textProviderType, typeof(LocalizationManager), autoInitialize, tryParents, refreshAvailableCultures, initializationCultureName, defaultInvariantCultureName)
        {
        }

        public ShinyLocalizationAttribute(Type textProviderType, Type localizationManagerType, bool autoInitialize = true, bool tryParents = true, bool refreshAvailableCultures = true, string? initializationCultureName = null, string? defaultInvariantCultureName = null)
        {
            this.TextProviderType = textProviderType;
            this.LocalizationManagerType = localizationManagerType;
            this.OptionsAction = optionsAction => optionsAction.WithAutoInitialization(autoInitialize, tryParents, refreshAvailableCultures, !initializationCultureName.IsEmpty() ? CultureInfo.CreateSpecificCulture(initializationCultureName) : null)
                .WithDefaultInvariantCulture(!defaultInvariantCultureName.IsEmpty()? CultureInfo.CreateSpecificCulture(defaultInvariantCultureName) : CultureInfo.InvariantCulture);
        }

        public Type TextProviderType { get; }
        public Type LocalizationManagerType { get; }
        public Action<LocalizationOptionsBuilder>? OptionsAction { get; }

        public override void Register(IServiceCollection services)
            => services.UseLocalization(this.TextProviderType, this.LocalizationManagerType, this.OptionsAction);
    }
}
