LessMarkup Engine
=================

* You have a serious team of developers, but do not have experience in web development?
* You have excellent web developers, but you know how difficult it is to make quality web solution with serious functional?
* You do not want to spend big resources on the development and maintenance of a web project?
* You already have working .NET solution and you want to add a web frontend?
* You are tired of fighting with HTML/CSS markup which is hard to test and lives its own life?

Then this is the solution for you! All you need is to make design of the main page. We have already done the remaining part of the work for you. You can continue to concentrate on your business logic, you do not need to think on how link any field on your page to the field in the database. You can forget about fighting with HTML and CSS.

Also you get ready-to-use modern web solution that runs using Model-View-Controller patern on the client and server. You can easily make a functional single-page application without applying considerable effort.

Used Technologies
=================

* AngularJS + Angular UI on the client side
* ASP.NET MVC on the server side

Main Features
=============

* Testability near to 100% as we use less markup than usual solutions, both on the client and server sides.
* Server modularity. Usual ASP.NET MVC application loads views only from main solution. We have fixed this problem - you can pack your views inside your assemblies. We use Inversion of control over all our code to achieve higher modularity, and it also improves testability of the code.
* Client modularity. We support AngularJS & RequireJS to create client-side modularity: only minimal set of javascript modules will be loaded on startup. All other javascript modules will be loaded by demand.
* Scalability. The engine can work on multiple hosts as it supports distributed caching.
* Callbacks. Changed data will be automatically pushed to the client page without any action from the client.
* Highly customizable. The engine does not make restrictions to you. You can use normal Razor views with any standard ASP.NET MVC functionality.
* Integrated language support.
* Two-stage login: the engine never sends your password during login.
* Customizable e-mail templates. Use Razor for templates to create excellent e-mails.
* Multi-site support. The engine automatically maps users to separate site based on host name.
* Automatic input form generation based on model defined in the code.
* Record lists (grids) based on ngGrid with automatic column definitions based on model in the code. No markup is required to create functional grids.
* Tree-based navigation support. Compare to flat navigation used in classic ASP.NET MVC.
* Consistent tree navigation support. You can start from any page in the tree - the page together with scripts and style files will be loaded only once and then the engine will reload only required parts of the pages during navigation.
* You can use normal ASP.NET MVC controllers

Please read project wiki for additional information: https://github.com/mvcdesign/less-markup/wiki

LessMarkup web site: http://www.lessmarkup.com

LessMarkup is distributed based on open source Mozilla Public License, version 2.0. You can obtain a copy of the license here: http://mozilla.org/MPL/2.0/