#Tridion Template Tests
This is a .NET project that allows automated template testing for Tridion component and page templates.

There's a proper article coming soon, but for now this read me will have to do.

##What it does
+ Performs automated tests for components and pages against all of the available templates.
+ It uses the Microsoft Visual Studio UnitTesting library to run "unit tests" against items and their templates using the Tridion core service.
+ The test project uses runsettings so it can be run against multiple environments either in Visual Studio or using vstest.console.exe (e.g. vstest.console.exe" "%WORKSPACE%\Website\Tridion.Template.Tests\bin\Debug\Tridion.Templates.Tests.dll" /Settings:"%WORKSPACE%\Tridion.Template.Tests\RunSettings\local.runsettings" /Logger:trx)
+ You can integrate it with Jenkins or [insert your perfered CI server] to automate the testing process.
+ You'll get nice output like this:

![](https://raw.githubusercontent.com/mtrl/Tridion.Template.Testing/master/Images/test-results.jpg)

![](https://raw.githubusercontent.com/mtrl/Tridion.Template.Testing/master/Images/console-output.jpg)

##How to use it
1. In your Tridion CM, create a Test folder in your desired publication, in here copy or create the components with the content that you want to test. A combination of components with and without all mandatory fields is a good start.
![](https://raw.githubusercontent.com/mtrl/Tridion.Template.Testing/master/Images/component-test-folder.jpg)
1. In your Tridion CM, create a Test structure group in your desired publication. This this SG create the pages that you want to test.
![](https://raw.githubusercontent.com/mtrl/Tridion.Template.Testing/master/Images/page-test-folder.jpg)
1. Open the project in Visual Studio and open the RunSettings/*.runsettings file
![](https://raw.githubusercontent.com/mtrl/Tridion.Template.Testing/master/Images/runsettings.jpg)
1. Change the values in this file to reflect your testing environment's set up
1. In the Test Explorer window, click the "Run all" button and watch in amazement as your templates are tested for you
![](https://raw.githubusercontent.com/mtrl/Tridion.Template.Testing/master/Images/run-all.jpg)
