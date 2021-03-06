using PhotoContest.Infrastructure.Interfaces;
using PhotoContest.Infrastructure.Services;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(PhotoContest.App.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(PhotoContest.App.NinjectWebCommon), "Stop")]

namespace PhotoContest.App
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;

    using PhotoContest.Data;
    using PhotoContest.Data.Interfaces;
    using PhotoContest.Data.UnitOfWork;

    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            kernel.Bind<IPhotoContestDbContext>().To<PhotoContestDbContext>();

            kernel.Bind<IUsersService>()
                .To<UsersService>();

            kernel.Bind<IContestsService>()
                .To<ContestService>();

            kernel.Bind<IStrategyService>()
                .To<StrategyService>();

            kernel.Bind<IPhotoContestData>()
                .To<PhotoContestData>()
                .WithConstructorArgument("context", new PhotoContestDbContext());
        }        
    }
}
