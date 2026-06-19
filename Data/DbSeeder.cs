using LMS.API.Models;

namespace LMS.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(LmsDbContext db)
    {
        if (db.Organizations.Any()) return;

        // ═══════════════════════════════════════════════════════
        //  ORGANIZATION
        // ═══════════════════════════════════════════════════════
        var org = new Organization
        {
            Name = "EKSHA TECHNOLOGIES",
            Slug = "eksha",
            PrimaryColor = "#1e40af",
            SecondaryColor = "#3b82f6",
            AccentColor = "#06b6d4",
            ThemeFont = "Plus Jakarta Sans",
            Tagline = "Empowering Careers Through Technology Education",
            PortalUrl = "https://scolared.com",
            Currency = "INR",
            IsActive = true
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════
        //  USERS
        // ═══════════════════════════════════════════════════════
        var pw = BCrypt.Net.BCrypt.HashPassword("Admin@123");

        var superAdmin = new User { FirstName = "Suresh", LastName = "Kumar", Email = "superadmin@lms.dev", PasswordHash = pw, Role = UserRole.SuperAdmin, OrganizationId = org.Id };
        var orgAdmin = new User { FirstName = "Priya", LastName = "Sharma", Email = "admin@eksha.in", PasswordHash = pw, Role = UserRole.OrgAdmin, OrganizationId = org.Id };
        db.Users.AddRange(superAdmin, orgAdmin);
        await db.SaveChangesAsync();

        var instructors = new[]
        {
            new User { FirstName = "Rajesh",  LastName = "Menon",    Email = "rajesh@eksha.in",  PasswordHash = pw, Role = UserRole.Instructor, OrganizationId = org.Id, Bio = "Senior Full Stack Engineer, 12 years experience, ex-Microsoft." },
            new User { FirstName = "Anjali",  LastName = "Reddy",    Email = "anjali@eksha.in",  PasswordHash = pw, Role = UserRole.Instructor, OrganizationId = org.Id, Bio = "Data Scientist & AI Researcher, IIT Madras alumna, 8 years experience." },
            new User { FirstName = "Vikram",  LastName = "Nair",     Email = "vikram@eksha.in",  PasswordHash = pw, Role = UserRole.Instructor, OrganizationId = org.Id, Bio = "Chartered Accountant & SAP Consultant, 15 years corporate finance experience." },
            new User { FirstName = "Deepa",   LastName = "Pillai",   Email = "deepa@eksha.in",   PasswordHash = pw, Role = UserRole.Instructor, OrganizationId = org.Id, Bio = "UI/UX Lead Designer, 9 years designing enterprise and consumer products." },
            new User { FirstName = "Arun",    LastName = "Krishnan", Email = "arun@eksha.in",    PasswordHash = pw, Role = UserRole.Instructor, OrganizationId = org.Id, Bio = "Cloud Architect & DevOps Engineer, AWS/Azure certified, 10 years experience." },
            new User { FirstName = "Nithya",  LastName = "Suresh",   Email = "nithya@eksha.in",  PasswordHash = pw, Role = UserRole.Instructor, OrganizationId = org.Id, Bio = "Digital Marketing Director, Google & Meta certified, 20+ brand campaigns." },
        };
        db.Users.AddRange(instructors);
        await db.SaveChangesAsync();

        var students = new[]
        {
            new User { FirstName = "Aarav",   LastName = "Gupta",   Email = "aarav@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Ishaan",  LastName = "Verma",   Email = "ishaan@gmail.com",  PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Ananya",  LastName = "Nair",    Email = "ananya@gmail.com",  PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Arjun",   LastName = "Reddy",   Email = "arjun@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Kavya",   LastName = "Iyer",    Email = "kavya@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Rohan",   LastName = "Joshi",   Email = "rohan@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Priya",   LastName = "Singh",   Email = "priya@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Rahul",   LastName = "Das",     Email = "rahul@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Sneha",   LastName = "Patel",   Email = "sneha@gmail.com",   PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
            new User { FirstName = "Nikhil",  LastName = "Rao",     Email = "nikhil@gmail.com",  PasswordHash = pw, Role = UserRole.Student, OrganizationId = org.Id },
        };
        db.Users.AddRange(students);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════
        //  DEPARTMENTS
        // ═══════════════════════════════════════════════════════
        var deptIT = new Department { Name = "Information Technology", Description = "Software development, programming, web technologies, mobile apps, and computer science fundamentals.", IconEmoji = "💻", Color = "#1e40af", OrganizationId = org.Id };
        var deptData = new Department { Name = "Data Science & AI", Description = "Data analysis, machine learning, artificial intelligence, statistics, and business intelligence.", IconEmoji = "📊", Color = "#7c3aed", OrganizationId = org.Id };
        var deptFin = new Department { Name = "Finance & Accounting", Description = "Financial management, accounting, investment analysis, SAP FICO, and corporate finance.", IconEmoji = "💰", Color = "#059669", OrganizationId = org.Id };
        var deptDes = new Department { Name = "Design & Creative", Description = "UI/UX design, graphic design, product design, Figma, Adobe tools, and design thinking.", IconEmoji = "🎨", Color = "#dc2626", OrganizationId = org.Id };
        var deptCld = new Department { Name = "Cloud & DevOps", Description = "AWS, Azure, GCP, Docker, Kubernetes, CI/CD pipelines, and infrastructure automation.", IconEmoji = "☁️", Color = "#0891b2", OrganizationId = org.Id };
        var deptMkt = new Department { Name = "Digital Marketing", Description = "SEO, SEM, social media marketing, content strategy, email marketing, and analytics.", IconEmoji = "📣", Color = "#ea580c", OrganizationId = org.Id };
        db.Departments.AddRange(deptIT, deptData, deptFin, deptDes, deptCld, deptMkt);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════
        //  CATEGORIES
        // ═══════════════════════════════════════════════════════
        var catWeb = new Category { Name = "Web Development", Description = "Full-stack web development with modern frameworks like React, Angular, Node.js, and .NET.", IconEmoji = "🌐", DepartmentId = deptIT.Id, OrganizationId = org.Id, DisplayOrder = 1 };
        var catMobile = new Category { Name = "Mobile Development", Description = "iOS and Android app development using React Native, Flutter, and native Swift/Kotlin.", IconEmoji = "📱", DepartmentId = deptIT.Id, OrganizationId = org.Id, DisplayOrder = 2 };
        var catDB = new Category { Name = "Databases & SQL", Description = "Relational and NoSQL databases including MySQL, PostgreSQL, MongoDB, and Redis.", IconEmoji = "🗄️", DepartmentId = deptIT.Id, OrganizationId = org.Id, DisplayOrder = 3 };
        var catCS = new Category { Name = "Cybersecurity", Description = "Ethical hacking, penetration testing, network security, and security certifications.", IconEmoji = "🔐", DepartmentId = deptIT.Id, OrganizationId = org.Id, DisplayOrder = 4 };
        var catML = new Category { Name = "Machine Learning & AI", Description = "Supervised learning, deep learning, NLP, computer vision, and production ML pipelines.", IconEmoji = "🤖", DepartmentId = deptData.Id, OrganizationId = org.Id, DisplayOrder = 1 };
        var catBI = new Category { Name = "Business Intelligence", Description = "Power BI, Tableau, data visualization, dashboards, and reporting for business decisions.", IconEmoji = "📈", DepartmentId = deptData.Id, OrganizationId = org.Id, DisplayOrder = 2 };
        var catPython = new Category { Name = "Python & Data Analysis", Description = "Python programming, NumPy, Pandas, data wrangling, and statistical analysis.", IconEmoji = "🐍", DepartmentId = deptData.Id, OrganizationId = org.Id, DisplayOrder = 3 };
        var catSAP = new Category { Name = "SAP ERP", Description = "SAP FICO, MM, SD, HANA, S/4HANA implementation and end-user training.", IconEmoji = "🏢", DepartmentId = deptFin.Id, OrganizationId = org.Id, DisplayOrder = 1 };
        var catAcct = new Category { Name = "Accounting & Finance", Description = "Financial accounting, management accounting, taxation, auditing, and investment fundamentals.", IconEmoji = "📒", DepartmentId = deptFin.Id, OrganizationId = org.Id, DisplayOrder = 2 };
        var catFigma = new Category { Name = "UI/UX & Figma", Description = "User interface design, UX research, wireframing, prototyping, and design systems in Figma.", IconEmoji = "✏️", DepartmentId = deptDes.Id, OrganizationId = org.Id, DisplayOrder = 1 };
        var catGraph = new Category { Name = "Graphic Design", Description = "Adobe Photoshop, Illustrator, InDesign, branding, typography, and visual communication.", IconEmoji = "🖌️", DepartmentId = deptDes.Id, OrganizationId = org.Id, DisplayOrder = 2 };
        var catAWS = new Category { Name = "AWS Cloud", Description = "Amazon Web Services core services, architecture, security, and AWS certification prep.", IconEmoji = "☁️", DepartmentId = deptCld.Id, OrganizationId = org.Id, DisplayOrder = 1 };
        var catDocker = new Category { Name = "Docker & Kubernetes", Description = "Containerization, orchestration, microservices deployment, and Helm charts.", IconEmoji = "🐳", DepartmentId = deptCld.Id, OrganizationId = org.Id, DisplayOrder = 2 };
        var catSEO = new Category { Name = "SEO & Content Marketing", Description = "Search engine optimization, keyword research, content strategy, and organic traffic growth.", IconEmoji = "🔍", DepartmentId = deptMkt.Id, OrganizationId = org.Id, DisplayOrder = 1 };
        var catSocial = new Category { Name = "Social Media Marketing", Description = "Facebook Ads, Instagram, LinkedIn marketing, influencer strategy, and paid campaigns.", IconEmoji = "📲", DepartmentId = deptMkt.Id, OrganizationId = org.Id, DisplayOrder = 2 };

        db.Categories.AddRange(catWeb, catMobile, catDB, catCS, catML, catBI, catPython, catSAP, catAcct, catFigma, catGraph, catAWS, catDocker, catSEO, catSocial);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════
        //  HELPER: create module + lessons with YouTube links
        // ═══════════════════════════════════════════════════════
        async Task<Module> AddModule(int courseId, string title, string desc, int order)
        {
            var m = new Module { Title = title, Description = desc, CourseId = courseId, DisplayOrder = order, IsPreview = order == 0 };
            db.Modules.Add(m); await db.SaveChangesAsync(); return m;
        }

        async Task AddLesson(int moduleId, string title, string description, string? ytUrl, int order, int durSecs = 600, bool isPreview = false)
        {
            var lessonType = ytUrl != null ? LessonType.Video : LessonType.Article;
            var lesson = new Lesson
            {
                Title = title,
                Description = description,
                Type = lessonType,
                ModuleId = moduleId,
                DisplayOrder = order,
                DurationSecs = durSecs,
                IsPreview = isPreview || order == 0,
                IsPublished = true,
                VideoUrl = ytUrl
            };
            if (ytUrl != null)
            {
                lesson.ContentBlocksJson = System.Text.Json.JsonSerializer.Serialize(new object[]
                {
                    new { type = "Video", order = 0, videoUrl = ytUrl, videoTitle = title, videoDurationSecs = durSecs },
                    new { type = "Text",  order = 1, textContent = $"<p>{description}</p><p>Watch the video above and take notes on the key concepts covered in this lesson.</p>" }
                });
            }
            else
            {
                lesson.ContentBlocksJson = System.Text.Json.JsonSerializer.Serialize(new object[]
                {
                    new { type = "Text", order = 0, textContent = $"<h2>{title}</h2><p>{description}</p><p>This lesson covers the theoretical aspects and practical examples of the topic. Review the material carefully before moving to the next lesson.</p>" }
                });
            }
            db.Lessons.Add(lesson); await db.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════
        //  COURSES WITH REAL LESSONS
        // ═══════════════════════════════════════════════════════

        // ── 1. Full Stack .NET 8 + React 18 ─────────────────
        var cNet = new Course
        {
            Title = "Full Stack Web Development: .NET 8 + React 18",
            Description = "<p>Master full-stack development using <strong>ASP.NET Core 8 Web API</strong> and <strong>React 18</strong> with TypeScript. Build production-ready applications from scratch.</p><p>This course covers REST APIs, Entity Framework Core, JWT authentication, React hooks, Tailwind CSS, and deployment on Azure.</p>",
            Level = CourseLevel.Intermediate,
            Status = CourseStatus.Published,
            Price = 3999,
            IsFree = false,
            Language = "English",
            Tags = "dotnet,react,typescript,fullstack,webdev",
            ThumbnailUrl = "https://images.unsplash.com/photo-1593720213428-28a5b9e94613?w=640",
            DurationMinutes = 2400,
            CategoryId = catWeb.Id,
            InstructorId = instructors[0].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cNet); await db.SaveChangesAsync();

        var m1 = await AddModule(cNet.Id, "C# & .NET 8 Fundamentals", "Refresh core C# concepts and explore what's new in .NET 8.", 0);
        await AddLesson(m1.Id, "What's New in .NET 8 & C# 12", "Overview of the latest .NET 8 features including primary constructors, collection expressions, and performance improvements.", "https://www.youtube.com/watch?v=mna5fg7QGz8", 0, 900, true);
        await AddLesson(m1.Id, "Setting Up Your Development Environment", "Install Visual Studio 2022, .NET 8 SDK, Node.js, and configure your workspace for full-stack development.", "https://www.youtube.com/watch?v=fmvcAzHpsk8", 1, 720);
        await AddLesson(m1.Id, "C# Records, Pattern Matching & LINQ", "Deep dive into records, switch expressions, pattern matching, and writing clean LINQ queries.", "https://www.youtube.com/watch?v=9XMTtRTX0DM", 2, 840);

        var m2 = await AddModule(cNet.Id, "ASP.NET Core 8 Web API", "Build robust RESTful APIs with controllers, minimal APIs, and middleware.", 1);
        await AddLesson(m2.Id, "Creating Your First REST API", "Scaffold a Web API project, understand the Program.cs model, and create your first controller with CRUD endpoints.", "https://www.youtube.com/watch?v=fmvcAzHpsk8", 0, 960);
        await AddLesson(m2.Id, "Entity Framework Core 8 & Code First Migrations", "Model your database with EF Core, write migrations, seed data, and use DbContext properly.", "https://www.youtube.com/watch?v=SryQxUeChMc", 1, 1080);
        await AddLesson(m2.Id, "JWT Authentication & Authorization", "Implement JSON Web Token authentication, role-based authorization, and secure your API endpoints.", "https://www.youtube.com/watch?v=mgeuh8k3SvQ", 2, 900);
        await AddLesson(m2.Id, "Error Handling & API Best Practices", "Global exception handling, problem details, validation with FluentValidation, and API versioning.", null, 3, 600);

        var m3 = await AddModule(cNet.Id, "React 18 & TypeScript", "Build modern React applications with hooks, context, and TypeScript.", 2);
        await AddLesson(m3.Id, "React 18 Fundamentals & JSX", "Understand React's component model, JSX syntax, rendering, and how React 18's concurrent features work.", "https://www.youtube.com/watch?v=CgkZ7MvWUAA", 0, 900);
        await AddLesson(m3.Id, "Hooks: useState, useEffect, useCallback, useMemo", "Master React hooks with real-world examples, dependency arrays, and performance optimization patterns.", "https://www.youtube.com/watch?v=TNhaISOUy6Q", 1, 1200);
        await AddLesson(m3.Id, "React Query & Axios for API Calls", "Integrate TanStack Query for server state management, caching, background refetching, and mutations.", "https://www.youtube.com/watch?v=novnyCaa7To", 2, 960);
        await AddLesson(m3.Id, "React Router v6 & Protected Routes", "Implement client-side routing, nested routes, params, and authentication guards.", null, 3, 720);

        var m4 = await AddModule(cNet.Id, "Full Stack Integration & Deployment", "Connect frontend to backend and deploy to production.", 3);
        await AddLesson(m4.Id, "CORS, Authentication Flow End-to-End", "Configure CORS policies, implement login/register flow connecting React frontend to .NET API.", null, 0, 840);
        await AddLesson(m4.Id, "File Upload with Cloudflare R2", "Build drag-and-drop file uploads, progress tracking, and integrate Cloudflare R2 object storage.", null, 1, 720);
        await AddLesson(m4.Id, "Deploying .NET API to Azure App Service", "Deploy your API to Azure, configure environment variables, set up CI/CD with GitHub Actions.", "https://www.youtube.com/watch?v=4BwyqmRTrx8", 2, 900);

        // ── 2. Python for Data Science & Machine Learning ───
        var cPy = new Course
        {
            Title = "Python for Data Science & Machine Learning",
            Description = "<p>Learn Python from the ground up and apply it to real data science problems. Master <strong>NumPy, Pandas, Matplotlib, Scikit-learn</strong>, and build ML models from scratch.</p><p>By the end you will be able to build, train, evaluate, and deploy machine learning models confidently.</p>",
            Level = CourseLevel.Beginner,
            Status = CourseStatus.Published,
            Price = 2999,
            IsFree = false,
            Language = "English",
            Tags = "python,datascience,machinelearning,numpy,pandas,sklearn",
            ThumbnailUrl = "https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=640",
            DurationMinutes = 3000,
            CategoryId = catPython.Id,
            InstructorId = instructors[1].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cPy); await db.SaveChangesAsync();

        var p1 = await AddModule(cPy.Id, "Python Programming Foundations", "Core Python syntax, data types, functions, OOP, and file handling.", 0);
        await AddLesson(p1.Id, "Python Installation & Your First Script", "Set up Python 3.12, VS Code, and virtual environments. Write and run your first Python program.", "https://www.youtube.com/watch?v=_uQrJ0TkZlc", 0, 600, true);
        await AddLesson(p1.Id, "Python Data Types & Control Flow", "Variables, lists, tuples, dictionaries, sets, loops, conditionals, and comprehensions.", "https://www.youtube.com/watch?v=rfscVS0vtbw", 1, 900);
        await AddLesson(p1.Id, "Functions, Modules & OOP in Python", "Define functions, import modules, write classes with inheritance and encapsulation.", "https://www.youtube.com/watch?v=Ej_02ICOIgs", 2, 840);

        var p2 = await AddModule(cPy.Id, "NumPy & Pandas for Data Analysis", "Numerical computing and data manipulation with industry-standard libraries.", 1);
        await AddLesson(p2.Id, "NumPy Arrays & Vectorized Operations", "Create arrays, reshape, index, slice, and perform fast mathematical operations without loops.", "https://www.youtube.com/watch?v=QUT1VHiLmmI", 0, 900);
        await AddLesson(p2.Id, "Pandas DataFrames: Load, Clean & Transform Data", "Read CSV/Excel files, handle missing data, filter rows, group by, merge and reshape datasets.", "https://www.youtube.com/watch?v=vmEHCJofslg", 1, 1080);
        await AddLesson(p2.Id, "Exploratory Data Analysis (EDA) with Matplotlib & Seaborn", "Create line charts, bar plots, scatter plots, heatmaps, and distribution plots for insight discovery.", "https://www.youtube.com/watch?v=3Xc3CA655Y4", 2, 960);

        var p3 = await AddModule(cPy.Id, "Machine Learning with Scikit-learn", "Classification, regression, clustering, and model evaluation.", 2);
        await AddLesson(p3.Id, "Machine Learning Concepts & Workflow", "Understand supervised vs unsupervised learning, train/test split, cross-validation, and the ML pipeline.", "https://www.youtube.com/watch?v=NWONeJKn6kc", 0, 900);
        await AddLesson(p3.Id, "Linear & Logistic Regression from Scratch", "Implement linear regression and logistic regression, understand gradient descent, and evaluate models.", null, 1, 1080);
        await AddLesson(p3.Id, "Decision Trees, Random Forest & XGBoost", "Tree-based algorithms, feature importance, ensemble methods, and hyperparameter tuning.", "https://www.youtube.com/watch?v=J4Wdy0Wc_xQ", 2, 1080);
        await AddLesson(p3.Id, "Model Evaluation: Precision, Recall, ROC-AUC", "Confusion matrix, precision-recall curves, ROC curves, and choosing the right metric for your problem.", null, 3, 720);

        var p4 = await AddModule(cPy.Id, "Deep Learning with TensorFlow & Keras", "Neural networks, CNNs, and deploying models.", 3);
        await AddLesson(p4.Id, "Neural Networks: Architecture & Backpropagation", "Perceptrons, activation functions, forward/backward propagation, and training with Keras.", "https://www.youtube.com/watch?v=aircAruvnKk", 0, 1200);
        await AddLesson(p4.Id, "Convolutional Neural Networks for Image Classification", "Build a CNN that classifies images, understand convolution, pooling, and transfer learning with ResNet.", "https://www.youtube.com/watch?v=YRhxdVk_sIs", 1, 1200);
        await AddLesson(p4.Id, "Saving, Loading & Deploying ML Models with FastAPI", "Export trained models, build a prediction API with FastAPI, and call it from a simple web frontend.", null, 2, 900);

        // ── 3. SAP FICO: Financial Accounting & Controlling ─
        var cSAP = new Course
        {
            Title = "SAP FICO: Financial Accounting & Controlling",
            Description = "<p>Master <strong>SAP FICO</strong> (FI - Financial Accounting + CO - Controlling) — the most in-demand SAP module. Learn configuration, end-user operations, and real business scenarios.</p><p>Covers General Ledger, Accounts Payable, Accounts Receivable, Asset Accounting, Cost Centers, and Profit Centers.</p>",
            Level = CourseLevel.Intermediate,
            Status = CourseStatus.Published,
            Price = 4999,
            IsFree = false,
            Language = "English",
            Tags = "sap,fico,erp,finance,accounting,s4hana",
            ThumbnailUrl = "https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?w=640",
            DurationMinutes = 3600,
            CategoryId = catSAP.Id,
            InstructorId = instructors[2].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cSAP); await db.SaveChangesAsync();

        var s1 = await AddModule(cSAP.Id, "SAP Basics & Navigation", "Understand SAP ERP, S/4HANA architecture, and navigate the system confidently.", 0);
        await AddLesson(s1.Id, "Introduction to SAP & ERP Systems", "What is ERP, how SAP fits in the enterprise landscape, and the difference between SAP ECC and S/4HANA.", "https://www.youtube.com/watch?v=7rQaLGYOuGo", 0, 900, true);
        await AddLesson(s1.Id, "SAP GUI Navigation & Basic Transactions", "Log in to SAP, use the menu structure, enter transaction codes, and customize your workspace.", "https://www.youtube.com/watch?v=oiWKjWB6O6A", 1, 720);
        await AddLesson(s1.Id, "Organizational Structure in SAP FICO", "Configure Company Code, Business Area, Chart of Accounts, and Fiscal Year Variant.", null, 2, 840);

        var s2 = await AddModule(cSAP.Id, "FI – Financial Accounting", "General Ledger, AP, AR, and Asset Accounting configuration and posting.", 1);
        await AddLesson(s2.Id, "General Ledger Accounting (FI-GL)", "Create G/L accounts, define posting keys, post journal entries, and run financial statements.", "https://www.youtube.com/watch?v=r9IFm4FZFM0", 0, 1200);
        await AddLesson(s2.Id, "Accounts Payable (FI-AP): Vendor Transactions", "Vendor master data, invoice posting, payment runs, and clearing open items.", null, 1, 1080);
        await AddLesson(s2.Id, "Accounts Receivable (FI-AR): Customer Transactions", "Customer master data, invoice posting, dunning, and incoming payment processing.", null, 2, 1080);
        await AddLesson(s2.Id, "Asset Accounting (FI-AA)", "Create asset master records, post acquisitions, run depreciation, and perform asset disposals.", null, 3, 960);

        var s3 = await AddModule(cSAP.Id, "CO – Controlling", "Cost center accounting, internal orders, and profitability analysis.", 2);
        await AddLesson(s3.Id, "Cost Center Accounting (CO-CCA)", "Define cost centers, post primary cost postings, allocate costs, and run cost center reports.", null, 0, 1080);
        await AddLesson(s3.Id, "Internal Orders & Budget Management", "Create internal orders, assign budgets, monitor spend, and settle costs to cost centers.", null, 1, 960);
        await AddLesson(s3.Id, "Profit Center Accounting & Reporting", "Configure profit centers, assign to business transactions, and generate profitability reports.", null, 2, 900);

        var s4 = await AddModule(cSAP.Id, "Integration & Reporting", "FI-CO integration, month-end closing, and SAP reporting tools.", 3);
        await AddLesson(s4.Id, "FI-CO Integration Points", "How financial accounting and controlling interact, reconciliation ledger, and real-time integration.", null, 0, 840);
        await AddLesson(s4.Id, "Month-End & Year-End Closing Activities", "Step-by-step guide to period-end closing: depreciation run, GR/IR clearing, balance carryforward.", null, 1, 1080);
        await AddLesson(s4.Id, "SAP Report Painter & Report Writer", "Build custom financial reports using SAP Report Painter without ABAP programming.", null, 2, 720);

        // ── 4. UI/UX Design & Figma Complete Course ─────────
        var cFigma = new Course
        {
            Title = "Complete UI/UX Design with Figma",
            Description = "<p>Learn <strong>UI/UX Design</strong> from scratch using Figma — the industry-standard design tool. Cover UX research, wireframing, prototyping, design systems, and handoff to developers.</p><p>Build a real portfolio project: a complete mobile app and web dashboard design.</p>",
            Level = CourseLevel.Beginner,
            Status = CourseStatus.Published,
            Price = 2499,
            IsFree = false,
            Language = "English",
            Tags = "figma,uxdesign,uiux,design,prototyping,wireframing",
            ThumbnailUrl = "https://images.unsplash.com/photo-1561070791-2526d30994b5?w=640",
            DurationMinutes = 2100,
            CategoryId = catFigma.Id,
            InstructorId = instructors[3].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cFigma); await db.SaveChangesAsync();

        var f1 = await AddModule(cFigma.Id, "Design Thinking & UX Foundations", "Understand users, problem framing, and the UX design process.", 0);
        await AddLesson(f1.Id, "What is UX Design? Principles & Process", "The 5-stage Design Thinking model: Empathize, Define, Ideate, Prototype, Test. Why UX matters for product success.", "https://www.youtube.com/watch?v=SRec90j6lTY", 0, 720, true);
        await AddLesson(f1.Id, "User Research Methods: Interviews & Surveys", "Plan and conduct user interviews, write survey questions, analyze qualitative data, and create user personas.", null, 1, 840);
        await AddLesson(f1.Id, "Information Architecture & User Flows", "Card sorting, site maps, user flow diagrams, and structuring navigation for intuitive experiences.", null, 2, 720);

        var f2 = await AddModule(cFigma.Id, "Figma Fundamentals", "Master Figma's tools, components, and auto-layout.", 1);
        await AddLesson(f2.Id, "Getting Started with Figma", "Create a Figma account, understand frames, layers, shapes, text, and the main interface panels.", "https://www.youtube.com/watch?v=t0aCoqXKFOU", 0, 840);
        await AddLesson(f2.Id, "Auto Layout & Responsive Design in Figma", "Use Auto Layout to build responsive components that adapt to content, just like CSS flexbox.", "https://www.youtube.com/watch?v=TyaGpGDFczw", 1, 900);
        await AddLesson(f2.Id, "Components, Variants & Design Systems", "Build reusable component libraries with variants, properties, and publish to your team library.", "https://www.youtube.com/watch?v=FTFaQWZBqQ8", 2, 1080);

        var f3 = await AddModule(cFigma.Id, "Wireframing & Prototyping", "Go from sketches to interactive prototypes.", 2);
        await AddLesson(f3.Id, "Low-Fidelity Wireframing Techniques", "Sketch layouts quickly, iterate fast, and use wireframe kits to structure pages before visual design.", null, 0, 720);
        await AddLesson(f3.Id, "High-Fidelity UI Design: Mobile App", "Design a complete mobile app with real typography, color systems, spacing, and visual hierarchy.", null, 1, 1200);
        await AddLesson(f3.Id, "Interactive Prototyping & User Testing", "Add interactions in Figma, create clickable prototypes, share with users, and analyze feedback.", null, 2, 840);

        var f4 = await AddModule(cFigma.Id, "Developer Handoff & Portfolio", "Export assets, write design specs, and build your portfolio.", 3);
        await AddLesson(f4.Id, "Developer Handoff with Figma Inspect", "Generate CSS snippets, export assets at 1x/2x/3x, document component specs for engineering teams.", null, 0, 600);
        await AddLesson(f4.Id, "Building a UX Portfolio that Gets Hired", "Structure your case studies, write compelling project stories, and present your design decisions.", null, 1, 720);

        // ── 5. AWS Cloud Practitioner + Solutions Architect ─
        var cAWS = new Course
        {
            Title = "AWS Cloud: From Zero to Solutions Architect",
            Description = "<p>Master <strong>Amazon Web Services</strong> from fundamentals to architecting scalable, secure, and cost-optimized solutions. Prepare for the <strong>AWS Cloud Practitioner</strong> and <strong>Solutions Architect Associate</strong> certifications.</p><p>Hands-on labs with EC2, S3, RDS, Lambda, VPC, IAM, CloudFormation, and more.</p>",
            Level = CourseLevel.Beginner,
            Status = CourseStatus.Published,
            Price = 3499,
            IsFree = false,
            Language = "English",
            Tags = "aws,cloud,ec2,s3,lambda,vpc,certification,devops",
            ThumbnailUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=640",
            DurationMinutes = 2700,
            CategoryId = catAWS.Id,
            InstructorId = instructors[4].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cAWS); await db.SaveChangesAsync();

        var a1 = await AddModule(cAWS.Id, "Cloud Fundamentals & AWS Core Concepts", "Understand cloud computing models, AWS global infrastructure, and core services.", 0);
        await AddLesson(a1.Id, "What is Cloud Computing? IaaS, PaaS, SaaS", "Cloud deployment models, benefits of cloud vs on-premise, and how major providers compare.", "https://www.youtube.com/watch?v=M988_fsOSWo", 0, 840, true);
        await AddLesson(a1.Id, "AWS Global Infrastructure: Regions, AZs & Edge Locations", "How AWS is structured globally, choosing the right region, and high-availability design principles.", "https://www.youtube.com/watch?v=a9__D53WsUs", 1, 720);
        await AddLesson(a1.Id, "IAM: Users, Roles, Policies & Best Practices", "Create IAM users, groups, and roles. Write policies, enable MFA, and follow the principle of least privilege.", "https://www.youtube.com/watch?v=3y596T1eH_8", 2, 960);

        var a2 = await AddModule(cAWS.Id, "Compute: EC2, Lambda & ECS", "Virtual machines, serverless functions, and container services.", 1);
        await AddLesson(a2.Id, "Amazon EC2: Launch, Configure & Secure Instances", "Launch EC2 instances, configure security groups, connect via SSH, and use user data scripts.", null, 0, 1080);
        await AddLesson(a2.Id, "AWS Lambda: Serverless Computing Deep Dive", "Write Lambda functions, configure triggers (API Gateway, S3, DynamoDB), manage cold starts and IAM roles.", "https://www.youtube.com/watch?v=eOBq__h4OJ4", 1, 960);
        await AddLesson(a2.Id, "Auto Scaling Groups & Elastic Load Balancing", "Configure ALB, target groups, health checks, and auto-scaling policies to handle traffic spikes.", null, 2, 900);

        var a3 = await AddModule(cAWS.Id, "Storage & Databases", "S3, RDS, DynamoDB, and data management on AWS.", 2);
        await AddLesson(a3.Id, "Amazon S3: Storage Classes, Buckets & Policies", "Create S3 buckets, set lifecycle rules, configure static website hosting, and secure with bucket policies.", "https://www.youtube.com/watch?v=tfU0JEZjcsg", 0, 900);
        await AddLesson(a3.Id, "Amazon RDS & Aurora: Managed Relational Databases", "Create RDS instances, configure Multi-AZ failover, read replicas, automated backups, and encryption.", null, 1, 960);
        await AddLesson(a3.Id, "DynamoDB: NoSQL at Scale", "Design DynamoDB tables, understand partition keys, GSIs, DynamoDB Streams, and capacity modes.", null, 2, 840);

        var a4 = await AddModule(cAWS.Id, "Networking, Security & Architecture", "VPC, CloudFront, WAF, and Well-Architected Framework.", 3);
        await AddLesson(a4.Id, "Amazon VPC: Subnets, Route Tables & NAT Gateways", "Design a production VPC with public/private subnets, internet gateways, NAT gateways, and NACLs.", null, 0, 1080);
        await AddLesson(a4.Id, "CloudFront CDN & Route 53 DNS", "Set up a CloudFront distribution for global content delivery, configure Route 53 with health checks.", null, 1, 840);
        await AddLesson(a4.Id, "AWS Well-Architected Framework", "Review the 6 pillars: Operational Excellence, Security, Reliability, Performance, Cost Optimization, Sustainability.", null, 2, 720);

        // ── 6. Digital Marketing Complete Course ─────────────
        var cMkt = new Course
        {
            Title = "Digital Marketing: SEO, Ads & Social Media",
            Description = "<p>A complete, practical guide to <strong>Digital Marketing</strong> covering SEO, Google Ads, Meta Ads, email marketing, content strategy, and analytics.</p><p>Learn how to drive traffic, generate leads, build brand awareness, and measure ROI — skills that are in demand across every industry.</p>",
            Level = CourseLevel.Beginner,
            Status = CourseStatus.Published,
            Price = 1999,
            IsFree = false,
            Language = "English",
            Tags = "seo,googleads,socialmedia,email,contentmarketing,digitalmarketing",
            ThumbnailUrl = "https://images.unsplash.com/photo-1432888498266-38ffec3eaf0a?w=640",
            DurationMinutes = 1800,
            CategoryId = catSEO.Id,
            InstructorId = instructors[5].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cMkt); await db.SaveChangesAsync();

        var mk1 = await AddModule(cMkt.Id, "Digital Marketing Foundations", "Understand the digital landscape and build a marketing strategy.", 0);
        await AddLesson(mk1.Id, "The Digital Marketing Ecosystem", "Overview of channels: SEO, PPC, social media, email, content, and affiliate. How they work together.", "https://www.youtube.com/watch?v=bixR-KIJKYM", 0, 720, true);
        await AddLesson(mk1.Id, "Building a Digital Marketing Strategy", "Set SMART goals, define your target audience, map the customer journey, and choose the right channels.", null, 1, 840);

        var mk2 = await AddModule(cMkt.Id, "SEO: Search Engine Optimization", "Rank higher on Google through technical SEO, on-page SEO, and link building.", 1);
        await AddLesson(mk2.Id, "How Search Engines Work & Keyword Research", "Google crawling and indexing, keyword intent, tools: Google Keyword Planner, Ahrefs, SEMrush.", "https://www.youtube.com/watch?v=DvwS7cV9GmQ", 0, 960);
        await AddLesson(mk2.Id, "On-Page SEO: Title Tags, Meta, Content Optimization", "Write SEO-optimized content, optimize title tags, meta descriptions, headers, images, and internal links.", null, 1, 840);
        await AddLesson(mk2.Id, "Technical SEO: Site Speed, Core Web Vitals & Schema", "Improve site speed, fix crawl errors, implement schema markup, and pass Core Web Vitals assessment.", null, 2, 900);
        await AddLesson(mk2.Id, "Link Building & Off-Page SEO Strategies", "Guest posting, HARO, digital PR, broken link building, and building domain authority safely.", null, 3, 720);

        var mk3 = await AddModule(cMkt.Id, "Google Ads & Meta Ads", "Run paid advertising campaigns that convert.", 2);
        await AddLesson(mk3.Id, "Google Ads: Search Campaigns from Scratch", "Campaign structure, match types, Quality Score, bidding strategies, ad copy, and conversion tracking.", "https://www.youtube.com/watch?v=lD1bqKFsLmo", 0, 1080);
        await AddLesson(mk3.Id, "Meta Ads: Facebook & Instagram Campaigns", "Business Manager setup, custom audiences, lookalike audiences, creative best practices, and ROAS tracking.", null, 1, 1080);
        await AddLesson(mk3.Id, "Remarketing & Conversion Rate Optimization", "Google Remarketing, Facebook Pixel, landing page optimization, A/B testing, and reducing bounce rate.", null, 2, 840);

        var mk4 = await AddModule(cMkt.Id, "Content, Email & Analytics", "Create compelling content and measure everything.", 3);
        await AddLesson(mk4.Id, "Content Marketing Strategy & Blogging", "Content pillars, editorial calendar, writing for SEO, repurposing content across channels.", null, 0, 720);
        await AddLesson(mk4.Id, "Email Marketing with Mailchimp & Automation", "Build email lists, design campaigns, write subject lines that get opened, and set up automation flows.", null, 1, 840);
        await AddLesson(mk4.Id, "Google Analytics 4 & Looker Studio Reports", "Set up GA4, understand events and conversions, build custom Looker Studio dashboards for clients.", "https://www.youtube.com/watch?v=gKoXYGHBnmg", 2, 960);

        // ── 7. Power BI for Business Intelligence ────────────
        var cBI = new Course
        {
            Title = "Power BI: Business Intelligence & Data Visualization",
            Description = "<p>Master <strong>Microsoft Power BI</strong> to transform raw data into stunning, interactive dashboards and business reports. Learn DAX, Power Query, data modeling, and publishing to Power BI Service.</p><p>Used by over 250,000 organizations worldwide — a must-have skill for analysts and managers.</p>",
            Level = CourseLevel.Beginner,
            Status = CourseStatus.Published,
            Price = 2499,
            IsFree = false,
            Language = "English",
            Tags = "powerbi,businessintelligence,dax,powerquery,datavisualization,reporting",
            ThumbnailUrl = "https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=640",
            DurationMinutes = 2100,
            CategoryId = catBI.Id,
            InstructorId = instructors[1].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cBI); await db.SaveChangesAsync();

        var b1 = await AddModule(cBI.Id, "Power BI Fundamentals", "Install, navigate, and connect to data sources in Power BI Desktop.", 0);
        await AddLesson(b1.Id, "Introduction to Power BI & Business Intelligence", "What is BI, how Power BI fits the Microsoft data stack, and a walkthrough of the Power BI Desktop interface.", "https://www.youtube.com/watch?v=ykvAWKML9Gk", 0, 840, true);
        await AddLesson(b1.Id, "Connecting to Data Sources: Excel, SQL, Web APIs", "Import data from Excel, connect to SQL Server, scrape web tables, and use the built-in connectors.", null, 1, 720);
        await AddLesson(b1.Id, "Power Query: Data Transformation & Cleaning", "Use Power Query Editor to remove duplicates, split columns, change data types, and merge tables.", null, 2, 960);

        var b2 = await AddModule(cBI.Id, "Data Modeling & DAX", "Design star schemas and write DAX measures for complex calculations.", 1);
        await AddLesson(b2.Id, "Star Schema & Relationship Modeling", "Design fact and dimension tables, create relationships, manage cardinality, and avoid circular dependencies.", null, 0, 900);
        await AddLesson(b2.Id, "DAX Fundamentals: Calculated Columns & Measures", "Write DAX expressions, understand evaluation context, and create calculated columns vs measures.", null, 1, 1080);
        await AddLesson(b2.Id, "Advanced DAX: Time Intelligence & CALCULATE", "YTD, QTD, MTD calculations, CALCULATE with filters, RANKX, and TOPN for complex business logic.", null, 2, 1080);

        var b3 = await AddModule(cBI.Id, "Visualizations & Dashboards", "Build beautiful, interactive reports that stakeholders love.", 2);
        await AddLesson(b3.Id, "Core Visuals: Bar, Line, Scatter, Maps, Cards", "Choose the right visual for your data, configure axes, legends, data labels, and conditional formatting.", null, 0, 840);
        await AddLesson(b3.Id, "Slicers, Drill-through & Cross-Filtering", "Add slicers, create drill-through pages, configure cross-filter interactions, and build navigation buttons.", null, 1, 720);
        await AddLesson(b3.Id, "Dashboard Design Principles & Storytelling with Data", "Color palettes, layout grid, font hierarchy, and structuring a report to guide the viewer's attention.", null, 2, 720);

        var b4 = await AddModule(cBI.Id, "Power BI Service & Sharing", "Publish and collaborate on Power BI Cloud.", 3);
        await AddLesson(b4.Id, "Publishing to Power BI Service & Workspaces", "Publish reports, create workspaces, set up scheduled refresh, and manage data gateway.", null, 0, 720);
        await AddLesson(b4.Id, "Row-Level Security & Access Management", "Configure RLS roles, test security, and share reports securely across teams and external clients.", null, 1, 600);

        // ── 8. Cybersecurity Fundamentals & Ethical Hacking ─
        var cSec = new Course
        {
            Title = "Cybersecurity & Ethical Hacking Fundamentals",
            Description = "<p>Learn <strong>Cybersecurity</strong> from the attacker's perspective to build better defenses. Covers network security, web application security, penetration testing methodology, and security best practices.</p><p>Use industry tools: Nmap, Metasploit, Burp Suite, Wireshark, and Kali Linux.</p>",
            Level = CourseLevel.Intermediate,
            Status = CourseStatus.Published,
            Price = 3999,
            IsFree = false,
            Language = "English",
            Tags = "cybersecurity,ethicalhacking,kalilinux,penetrationtesting,networksecurity",
            ThumbnailUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=640",
            DurationMinutes = 2400,
            CategoryId = catCS.Id,
            InstructorId = instructors[0].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cSec); await db.SaveChangesAsync();

        var sec1 = await AddModule(cSec.Id, "Cybersecurity Foundations", "Core concepts: CIA triad, attack types, threat landscape, and setting up your lab.", 0);
        await AddLesson(sec1.Id, "Introduction to Cybersecurity & Ethical Hacking", "CIA Triad, types of hackers, legal framework, bug bounty programs, and setting up Kali Linux in VirtualBox.", "https://www.youtube.com/watch?v=hXSFdwIOfnE", 0, 900, true);
        await AddLesson(sec1.Id, "Networking for Hackers: TCP/IP, DNS & HTTP", "OSI model, TCP/IP stack, DNS resolution, HTTP/HTTPS, ports, and how traffic flows on a network.", "https://www.youtube.com/watch?v=3QhU9jd03a0", 1, 960);
        await AddLesson(sec1.Id, "Linux Command Line for Security Professionals", "File system navigation, permissions, process management, scripting basics, and essential security tools.", null, 2, 840);

        var sec2 = await AddModule(cSec.Id, "Reconnaissance & Scanning", "Information gathering and network scanning techniques.", 1);
        await AddLesson(sec2.Id, "Passive Reconnaissance: OSINT Techniques", "Google dorking, Shodan, WHOIS, LinkedIn OSINT, and building a target profile without touching the system.", null, 0, 840);
        await AddLesson(sec2.Id, "Active Scanning with Nmap", "Host discovery, port scanning, service version detection, OS fingerprinting, and Nmap scripting engine (NSE).", null, 1, 1080);
        await AddLesson(sec2.Id, "Vulnerability Scanning with Nessus & OpenVAS", "Configure and run vulnerability scans, interpret results, prioritize findings by CVSS score.", null, 2, 900);

        var sec3 = await AddModule(cSec.Id, "Exploitation & Post-Exploitation", "Metasploit, privilege escalation, and maintaining access.", 2);
        await AddLesson(sec3.Id, "Metasploit Framework: Exploitation Basics", "Metasploit architecture, searching for modules, configuring exploits, and working with Meterpreter.", null, 0, 1080);
        await AddLesson(sec3.Id, "Privilege Escalation on Linux & Windows", "SUID exploits, weak file permissions, registry exploits, token impersonation, and automated enumeration tools.", null, 1, 1080);
        await AddLesson(sec3.Id, "Web Application Security: OWASP Top 10", "SQL injection, XSS, CSRF, broken authentication, insecure deserialization, and SSRF with Burp Suite demos.", null, 2, 1200);

        // ── 9. Free Intro Course ─────────────────────────────
        var cFree = new Course
        {
            Title = "Introduction to Programming: Think Like a Developer",
            Description = "<p>Completely <strong>free</strong> introduction to programming concepts. Learn computational thinking, algorithms, basic Python, and how software is built — no prior experience required.</p><p>Perfect starting point before enrolling in any technical course at EKSHA TECHNOLOGIES.</p>",
            Level = CourseLevel.Beginner,
            Status = CourseStatus.Published,
            Price = 0,
            IsFree = true,
            Language = "English",
            Tags = "programming,beginner,python,algorithms,free",
            ThumbnailUrl = "https://images.unsplash.com/photo-1516116216624-53e697fedbea?w=640",
            DurationMinutes = 600,
            CategoryId = catWeb.Id,
            InstructorId = instructors[0].Id,
            OrganizationId = org.Id
        };
        db.Courses.Add(cFree); await db.SaveChangesAsync();

        var fr1 = await AddModule(cFree.Id, "Programming Fundamentals", "Core concepts every developer must know.", 0);
        await AddLesson(fr1.Id, "What is Programming? How Computers Think", "Binary, how CPUs execute instructions, source code to machine code, and why programming is a superpower.", "https://www.youtube.com/watch?v=zOjov-2OZ0E", 0, 720, true);
        await AddLesson(fr1.Id, "Algorithms & Pseudocode", "Breaking problems into steps, writing pseudocode, flowcharts, time complexity basics (Big O notation).", "https://www.youtube.com/watch?v=KEkrWRHCDQU", 1, 840, true);
        await AddLesson(fr1.Id, "Your First Python Program & Variables", "Install Python, write Hello World, understand variables, data types, input/output, and basic operations.", "https://www.youtube.com/watch?v=_uQrJ0TkZlc", 2, 780, true);
        await AddLesson(fr1.Id, "Conditionals, Loops & Functions", "If/else statements, for/while loops, writing reusable functions, and understanding scope.", "https://www.youtube.com/watch?v=rfscVS0vtbw", 3, 840, true);
        await AddLesson(fr1.Id, "What to Learn Next: Your Developer Roadmap", "Overview of career paths: frontend, backend, data science, mobile, cloud. How to choose your specialization.", null, 4, 600, true);

        // ═══════════════════════════════════════════════════════
        //  MOCK TEST
        // ═══════════════════════════════════════════════════════
        var mt = new MockTest
        {
            Title = "Full Stack Developer Assessment",
            Description = "Assess your knowledge of web development, databases, APIs, and core programming concepts.",
            Topic = "Web Development",
            Difficulty = MockTestDifficulty.Mixed,
            Status = MockTestStatus.Published,
            TimeLimitMins = 30,
            TotalQuestions = 10,
            PassMarkPercent = 60,
            RandomizeQuestions = true,
            ShowResultImmediately = true,
            MaxAttempts = 3,
            Tags = "webdev,javascript,csharp,sql",
            OrganizationId = org.Id,
            CourseId = cNet.Id,
            CreatedById = instructors[0].Id
        };
        db.MockTests.Add(mt);
        await db.SaveChangesAsync();

        var questions = new[]
        {
            new MockTestQuestion
            {
                Text = "Which HTTP method should be used to partially update an existing resource in a REST API?",
                Topic = "REST APIs", QuestionType = MockQuestionType.SingleChoice,
                Difficulty = MockTestDifficulty.Easy, Marks = 1, MockTestId = mt.Id, DisplayOrder = 0,
                Explanation = "PATCH is used for partial updates. PUT replaces the entire resource. POST creates a new resource. DELETE removes it."
            },
            new MockTestQuestion
            {
                Text = "Which of the following are valid HTTP status codes for a successful response?",
                Topic = "REST APIs", QuestionType = MockQuestionType.MultiChoice,
                Difficulty = MockTestDifficulty.Easy, Marks = 2, MockTestId = mt.Id, DisplayOrder = 1,
                Explanation = "2xx codes indicate success. 200 OK, 201 Created, and 204 No Content are all valid success responses."
            },
            new MockTestQuestion
            {
                Text = "What does the following SQL query return?\nSELECT COUNT(*) FROM orders WHERE status = 'completed'",
                Topic = "SQL", QuestionType = MockQuestionType.Dropdown,
                Difficulty = MockTestDifficulty.Easy, Marks = 1, MockTestId = mt.Id, DisplayOrder = 2,
                Explanation = "COUNT(*) returns the number of rows matching the WHERE condition."
            },
            new MockTestQuestion
            {
                Text = "React's useEffect hook runs after every render by default.",
                Topic = "React", QuestionType = MockQuestionType.TrueFalse,
                Difficulty = MockTestDifficulty.Easy, Marks = 1, MockTestId = mt.Id, DisplayOrder = 3,
                Explanation = "True. By default, useEffect runs after every render. Pass an empty array [] as the second argument to run only on mount."
            },
            new MockTestQuestion
            {
                Text = "What is the time complexity of searching for an element in a balanced binary search tree?",
                Topic = "Algorithms", QuestionType = MockQuestionType.SingleChoice,
                FormulaLatex = @"T(n) = O(\log n)",
                Difficulty = MockTestDifficulty.Medium, Marks = 2, MockTestId = mt.Id, DisplayOrder = 4,
                Explanation = "A balanced BST has height log n, so each search operation traverses at most log n nodes."
            },
            new MockTestQuestion
            {
                Text = "In C#, what is the difference between 'ref' and 'out' parameters?",
                Topic = "C#", QuestionType = MockQuestionType.ShortAnswer,
                Difficulty = MockTestDifficulty.Medium, Marks = 3, MockTestId = mt.Id, DisplayOrder = 5,
                Explanation = "'ref' requires the variable to be initialized before passing. 'out' does not require initialization but the method must assign it before returning."
            },
            new MockTestQuestion
            {
                Text = "Which React hook should you use to avoid recreating a function on every render when passing it to a child component?",
                Topic = "React", QuestionType = MockQuestionType.SingleChoice,
                Difficulty = MockTestDifficulty.Medium, Marks = 1, MockTestId = mt.Id, DisplayOrder = 6,
                Explanation = "useCallback memoizes the function reference so it only changes when its dependencies change, preventing unnecessary re-renders of child components."
            },
            new MockTestQuestion
            {
                Text = "JWT tokens are encrypted by default and cannot be read without a secret key.",
                Topic = "Security", QuestionType = MockQuestionType.TrueFalse,
                Difficulty = MockTestDifficulty.Medium, Marks = 1, MockTestId = mt.Id, DisplayOrder = 7,
                Explanation = "False. JWTs are base64 encoded (not encrypted) by default. Anyone can decode and read the payload. Only the signature is verified using the secret."
            },
            new MockTestQuestion
            {
                Text = "Which of the following are features of Entity Framework Core?",
                Topic = "EF Core", QuestionType = MockQuestionType.MultiChoice,
                Difficulty = MockTestDifficulty.Medium, Marks = 2, MockTestId = mt.Id, DisplayOrder = 8,
                Explanation = "EF Core provides Code First migrations, LINQ queries, change tracking, and supports multiple database providers."
            },
            new MockTestQuestion
            {
                Text = "What SQL clause is used to filter the results of a GROUP BY statement?",
                Topic = "SQL", QuestionType = MockQuestionType.Dropdown,
                Difficulty = MockTestDifficulty.Easy, Marks = 1, MockTestId = mt.Id, DisplayOrder = 9,
                Explanation = "HAVING filters grouped results, while WHERE filters individual rows before grouping."
            },
        };
        db.MockTestQuestions.AddRange(questions);
        await db.SaveChangesAsync();

        // Options for each question
        var opts = new List<MockTestOption>
        {
            // Q0: PATCH
            new() { Text="GET",    IsCorrect=false, DisplayOrder=0, QuestionId=questions[0].Id },
            new() { Text="POST",   IsCorrect=false, DisplayOrder=1, QuestionId=questions[0].Id },
            new() { Text="PATCH",  IsCorrect=true,  DisplayOrder=2, QuestionId=questions[0].Id },
            new() { Text="PUT",    IsCorrect=false, DisplayOrder=3, QuestionId=questions[0].Id },
            // Q1: multi 200,201,204
            new() { Text="200 OK",           IsCorrect=true,  DisplayOrder=0, QuestionId=questions[1].Id },
            new() { Text="201 Created",       IsCorrect=true,  DisplayOrder=1, QuestionId=questions[1].Id },
            new() { Text="204 No Content",    IsCorrect=true,  DisplayOrder=2, QuestionId=questions[1].Id },
            new() { Text="302 Found",         IsCorrect=false, DisplayOrder=3, QuestionId=questions[1].Id },
            new() { Text="404 Not Found",     IsCorrect=false, DisplayOrder=4, QuestionId=questions[1].Id },
            // Q2: dropdown
            new() { Text="The first completed order",  IsCorrect=false, DisplayOrder=0, QuestionId=questions[2].Id },
            new() { Text="All completed orders",       IsCorrect=false, DisplayOrder=1, QuestionId=questions[2].Id },
            new() { Text="The number of completed orders", IsCorrect=true, DisplayOrder=2, QuestionId=questions[2].Id },
            new() { Text="An error",                   IsCorrect=false, DisplayOrder=3, QuestionId=questions[2].Id },
            // Q3: TrueFalse
            new() { Text="True",  IsCorrect=true,  DisplayOrder=0, QuestionId=questions[3].Id },
            new() { Text="False", IsCorrect=false, DisplayOrder=1, QuestionId=questions[3].Id },
            // Q4: BST
            new() { Text="O(1)",      IsCorrect=false, DisplayOrder=0, QuestionId=questions[4].Id },
            new() { Text="O(log n)",  IsCorrect=true,  DisplayOrder=1, QuestionId=questions[4].Id },
            new() { Text="O(n)",      IsCorrect=false, DisplayOrder=2, QuestionId=questions[4].Id },
            new() { Text="O(n log n)",IsCorrect=false, DisplayOrder=3, QuestionId=questions[4].Id },
            // Q5: ShortAnswer (no options)
            // Q6: useCallback
            new() { Text="useState",     IsCorrect=false, DisplayOrder=0, QuestionId=questions[6].Id },
            new() { Text="useEffect",    IsCorrect=false, DisplayOrder=1, QuestionId=questions[6].Id },
            new() { Text="useCallback",  IsCorrect=true,  DisplayOrder=2, QuestionId=questions[6].Id },
            new() { Text="useMemo",      IsCorrect=false, DisplayOrder=3, QuestionId=questions[6].Id },
            // Q7: TrueFalse JWT
            new() { Text="True",  IsCorrect=false, DisplayOrder=0, QuestionId=questions[7].Id },
            new() { Text="False", IsCorrect=true,  DisplayOrder=1, QuestionId=questions[7].Id },
            // Q8: EF Core multi
            new() { Text="Code First migrations",       IsCorrect=true,  DisplayOrder=0, QuestionId=questions[8].Id },
            new() { Text="LINQ query translation",       IsCorrect=true,  DisplayOrder=1, QuestionId=questions[8].Id },
            new() { Text="Change tracking",             IsCorrect=true,  DisplayOrder=2, QuestionId=questions[8].Id },
            new() { Text="Built-in REST API generation", IsCorrect=false, DisplayOrder=3, QuestionId=questions[8].Id },
            // Q9: HAVING
            new() { Text="WHERE",   IsCorrect=false, DisplayOrder=0, QuestionId=questions[9].Id },
            new() { Text="HAVING",  IsCorrect=true,  DisplayOrder=1, QuestionId=questions[9].Id },
            new() { Text="FILTER",  IsCorrect=false, DisplayOrder=2, QuestionId=questions[9].Id },
            new() { Text="GROUP BY",IsCorrect=false, DisplayOrder=3, QuestionId=questions[9].Id },
        };
        db.MockTestOptions.AddRange(opts);
        await db.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════════════
        // SEED DATA — Training Batches, Live Classes, Assignment Submissions
        // Paste this block inside DbSeeder.cs, after courses/users/enrollments are
        // already seeded (it needs CourseId, OrganizationId, and student UserIds).
        // ════════════════════════════════════════════════════════════════════════════

        if (!db.TrainingBatches.Any())
        {
            var org1 = db.Organizations.First();
            var admin = db.Users.First(u => u.Role == UserRole.OrgAdmin || u.Role == UserRole.SuperAdmin);
            var course = db.Courses.FirstOrDefault(c => c.OrganizationId == org1.Id);
            var students1 = db.Users.Where(u => u.Role == UserRole.Student && u.OrganizationId == org1.Id).Take(5).ToList();

            // ── Training Batches ───────────────────────────────────────────────
            var batch1 = new TrainingBatch
            {
                BatchName = "Full Stack Development — Batch 7",
                Description = "Evening batch covering React, .NET, and MySQL",
                StartDate = DateTime.UtcNow.AddDays(-10),       // already started → Active
                DurationDays = 60,
                TotalFee = 25000,
                Notes = "Includes live project + certification",
                OrganizationId = org.Id,
                CourseId = course?.Id,
                CreatedById = admin.Id,
                Status = BatchStatus.Active,
            };

            var batch2 = new TrainingBatch
            {
                BatchName = "Data Analytics Bootcamp — Batch 3",
                Description = "Weekend batch — Excel, SQL, Power BI",
                StartDate = DateTime.UtcNow.AddDays(7),          // starts next week → Upcoming
                DurationDays = 45,
                TotalFee = 18000,
                Notes = "Beginner friendly, no prior experience needed",
                OrganizationId = org.Id,
                CourseId = course?.Id,
                CreatedById = admin.Id,
                Status = BatchStatus.Upcoming,
            };

            var batch3 = new TrainingBatch
            {
                BatchName = "UI/UX Design Sprint — Batch 1",
                Description = "Free intro batch on Figma & design thinking",
                StartDate = DateTime.UtcNow.AddDays(-45),
                DurationDays = 30,                                  // ended → Completed
                TotalFee = 0,
                OrganizationId = org.Id,
                CourseId = course?.Id,
                CreatedById = admin.Id,
                Status = BatchStatus.Completed,
            };

            db.TrainingBatches.AddRange(batch1, batch2, batch3);
            db.SaveChanges();

            // ── Batch students (mix of existing users + guests) ────────────────
            var batchStudents = new List<BatchStudent>();
            for (int i = 0; i < students1.Count; i++)
            {
                var paid = i % 3 == 0 ? 25000 : i % 3 == 1 ? 12000 : 0;
                batchStudents.Add(new BatchStudent
                {
                    BatchId = batch1.Id,
                    UserId = students[i].Id,
                    TotalFee = 25000,
                    PaidAmount = paid,
                    PaymentStatus = paid >= 25000 ? PaymentStatus.FullyPaid
                                   : paid > 0 ? PaymentStatus.PartiallyPaid
                                   : PaymentStatus.Pending,
                    Status = BatchStudentStatus.Active,
                    JoinedAt = batch1.StartDate.AddDays(-1),
                });
            }
            // Guest students (no account) for batch2
            batchStudents.Add(new BatchStudent
            {
                BatchId = batch2.Id,
                GuestName = "Priya Sharma",
                GuestEmail = "priya.sharma@example.com",
                GuestMobile = "9876543210",
                TotalFee = 18000,
                PaidAmount = 9000,
                PaymentStatus = PaymentStatus.PartiallyPaid,
                Status = BatchStudentStatus.Active,
            });
            batchStudents.Add(new BatchStudent
            {
                BatchId = batch2.Id,
                GuestName = "Rahul Verma",
                GuestEmail = "rahul.verma@example.com",
                GuestMobile = "9876501234",
                TotalFee = 18000,
                PaidAmount = 0,
                PaymentStatus = PaymentStatus.Pending,
                Status = BatchStudentStatus.Active,
            });
            db.BatchStudents.AddRange(batchStudents);
            db.SaveChanges();

            // ── Live Classes (Zoom/Meet sessions) ───────────────────────────────
            if (course is not null)
            {
                var liveClasses = new List<LiveClass>
        {
            new() {
                Title = "Live Q&A — React Hooks Deep Dive",
                Description = "Open doubt-clearing session on useEffect, useMemo, custom hooks",
                ScheduledAt = DateTime.UtcNow.AddDays(2).Date.AddHours(19),
                DurationMinutes = 60,
                Platform = LiveClassPlatform.Zoom,
                MeetingLink = "https://zoom.us/j/1234567890",
                MeetingId = "123 456 7890",
                MeetingPassword = "eksha123",
                CourseId = course.Id,
                HostId = admin.Id,
                Status = LiveClassStatus.Scheduled,
            },
            new() {
                Title = "Live Coding — Building a REST API with .NET 8",
                Description = "Hands-on walkthrough building CRUD endpoints from scratch",
                ScheduledAt = DateTime.UtcNow.AddDays(5).Date.AddHours(18),
                DurationMinutes = 90,
                Platform = LiveClassPlatform.GoogleMeet,
                MeetingLink = "https://meet.google.com/abc-defg-hij",
                CourseId = course.Id,
                HostId = admin.Id,
                Status = LiveClassStatus.Scheduled,
            },
            new() {
                Title = "Recorded Session — Intro to MySQL",
                Description = "Past session, recording available",
                ScheduledAt = DateTime.UtcNow.AddDays(-7),
                DurationMinutes = 75,
                Platform = LiveClassPlatform.Zoom,
                RecordingUrl = "https://example.com/recordings/mysql-intro.mp4",
                CourseId = course.Id,
                HostId = admin.Id,
                Status = LiveClassStatus.Completed,
                EmailSent = true,
            },
        };
                db.LiveClasses.AddRange(liveClasses);
                db.SaveChanges();

                // Auto-add enrolled students as attendees for the upcoming sessions
                foreach (var lc in liveClasses.Where(l => l.Status == LiveClassStatus.Scheduled))
                    foreach (var s in students)
                        db.LiveClassAttendees.Add(new LiveClassAttendee { LiveClassId = lc.Id, UserId = s.Id });
                db.SaveChanges();
            }

            Console.WriteLine($"Seeded {db.TrainingBatches.Count()} training batches, " +
                               $"{db.BatchStudents.Count()} batch students, " +
                               $"{db.LiveClasses.Count()} live classes.");
        }

        // ════════════════════════════════════════════════════════════════════════════
        // SEED — Assignment + submissions in various states (for testing the
        // submit → grade → request-resubmit → resubmit flow end-to-end)
        // ════════════════════════════════════════════════════════════════════════════

        if (!db.Assignments.Any())
        {
            var org2 = db.Organizations.First();
            var admin = db.Users.First(u => u.Role == UserRole.OrgAdmin || u.Role == UserRole.SuperAdmin || u.Role == UserRole.Instructor);
            var course = db.Courses.FirstOrDefault(c => c.OrganizationId == org2.Id);
            var students2 = db.Users.Where(u => u.Role == UserRole.Student && u.OrganizationId == org2.Id).Take(4).ToList();

            if (course is not null && students2.Count >= 4)
            {
                var assignment1 = new Assignment
                {
                    Title = "Module 1 Exercise — Variables & Loops",
                    Description = "Write 5 small programs demonstrating for/while loops and variable scope. Submit your code as a GitHub gist or Drive link.",
                    MaxMarks = 100,
                    DueDate = DateTime.UtcNow.AddDays(5),
                    CourseId = course.Id,
                    CreatedById = admin.Id,
                    Status = AssignmentStatus.Published,
                };

                var assignment2 = new Assignment
                {
                    Title = "Final Project — Build a Mini CRUD App",
                    Description = "Build a small full-stack app (any stack) with Create, Read, Update, Delete on one resource. Include a README.",
                    MaxMarks = 100,
                    DueDate = DateTime.UtcNow.AddDays(-2),     // overdue — for testing 'Late' status
                    CourseId = course.Id,
                    CreatedById = admin.Id,
                    Status = AssignmentStatus.Published,
                };

                db.Assignments.AddRange(assignment1, assignment2);
                db.SaveChanges();

                // ── Submission states across the 4 students ─────────────────────
                // Student[0]: Not submitted yet (NotSubmitted — no row at all)
                // Student[1]: Submitted, awaiting grading
                var sub1 = new AssignmentSubmission
                {
                    AssignmentId = assignment1.Id,
                    StudentId = students[1].Id,
                    SubmissionText = "Completed all 5 exercises, link to gist below.",
                    FileUrl = "https://gist.github.com/example/loops-exercise",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Status = SubmissionStatus.Submitted,
                };

                // Student[2]: Graded with feedback
                var sub2 = new AssignmentSubmission
                {
                    AssignmentId = assignment1.Id,
                    StudentId = students[2].Id,
                    SubmissionText = "All exercises done, added bonus recursion example.",
                    FileUrl = "https://github.com/example/student2-loops",
                    SubmittedAt = DateTime.UtcNow.AddDays(-3),
                    Status = SubmissionStatus.Graded,
                    MarksObtained = 92,
                    Feedback = "Excellent work! Clean code and the bonus recursion example was a nice touch.",
                    GradedById = admin.Id,
                    GradedAt = DateTime.UtcNow.AddDays(-2),
                };

                // Student[3]: Resubmission requested (no grade yet, needs to fix and resubmit)
                var sub3 = new AssignmentSubmission
                {
                    AssignmentId = assignment1.Id,
                    StudentId = students[3].Id,
                    SubmissionText = "Submitted 3 out of 5 exercises.",
                    FileUrl = "https://github.com/example/student3-incomplete",
                    SubmittedAt = DateTime.UtcNow.AddDays(-4),
                    Status = SubmissionStatus.ResubmitRequested,
                    Feedback = "You're missing exercises 4 and 5. Please complete them and resubmit.",
                    GradedById = admin.Id,
                    GradedAt = DateTime.UtcNow.AddDays(-3),
                };

                // Second assignment: one late submission
                var sub4 = new AssignmentSubmission
                {
                    AssignmentId = assignment2.Id,
                    StudentId = students[1].Id,
                    SubmissionText = "Built a simple Todo CRUD app using React + Express.",
                    FileUrl = "https://github.com/example/todo-crud",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),    // after due date → Late
                    Status = SubmissionStatus.Late,
                };

                db.AssignmentSubmissions.AddRange(sub1, sub2, sub3, sub4);
                db.SaveChanges();

                Console.WriteLine($"Seeded 2 assignments with 4 submissions in mixed states " +
                                   "(NotSubmitted, Submitted, Graded, ResubmitRequested, Late).");
            }
        }
        // ════════════════════════════════════════════════════════════════════════════
        // SEED — Sample About Us / Contact Us content + template selection
        // Paste inside DbSeeder.cs after the Organization is created.
        // ════════════════════════════════════════════════════════════════════════════

        var orgToUpdate = db.Organizations.First();
        if (string.IsNullOrEmpty(orgToUpdate.AboutUsContent))
        {
            orgToUpdate.ShowAboutUs = true;
            orgToUpdate.AboutUsTemplate = "split";   // try: classic | split | timeline | card
            orgToUpdate.AboutUsContent =
                "<p>Founded with a mission to make quality education accessible to everyone, " +
                "<strong>" + orgToUpdate.Name + "</strong> has trained thousands of students across " +
                "technology, design, and business domains.</p>" +
                "<p>Our instructors are industry practitioners who bring real-world project experience " +
                "into every classroom session.</p>" +
                "<p>We combine structured curriculum with hands-on mentorship to ensure every student " +
                "is job-ready by the time they graduate.</p>";

            orgToUpdate.ShowContactUs = true;
            orgToUpdate.ContactUsTemplate = "classic";  // try: classic | split | minimal | map-focus
            orgToUpdate.ContactEmail = "info@" + orgToUpdate.Slug + ".com";
            orgToUpdate.ContactPhone = "+91 98765 43210";
            orgToUpdate.ContactAddress = "3rd Floor, Tech Park,\nWhitefield, Bengaluru,\nKarnataka 560066";
            orgToUpdate.ContactMapEmbed = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3888.9!2d77.7499!3d12.9698!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x0%3A0x0!2zMTLCsDU4JzExLjMiTiA3N8KwNDQnNTkuNiJF!5e0!3m2!1sen!2sin!4v1";

            orgToUpdate.ShowScrollingBanner = true;
            orgToUpdate.ScrollingBannerText = "🎉 New Batch Starting Soon | Limited Seats Available | Enroll Today";

            orgToUpdate.ShowReferralOffer = true;
            orgToUpdate.ReferralOfferText = "Earn ₹2,500 for every friend who enrolls!";

            db.SaveChanges();
            Console.WriteLine("Seeded About Us / Contact Us content + templates for " + orgToUpdate.Name);
        }
    }

}