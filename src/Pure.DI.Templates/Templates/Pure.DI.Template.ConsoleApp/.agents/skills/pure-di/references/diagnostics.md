# Generator diagnostics

Run `dotnet build` for the affected project and start with the first Pure.DI diagnostic. The stable diagnostic reference maps IDs to causes and fixes: https://github.com/DevTeam/Pure.DI/blob/master/DIAGNOSTICS.md . Match guidance to the package version in the project.

For `DIE000`, find the unresolved contract and injection site. Add an appropriate binding for an abstraction, or verify that the intended concrete type can be auto-bound. For `DIE001`, inspect incomplete or conflicting binding metadata. For `DIE002`, resolve the underlying C# error first. For invalid composition or root names, use valid C# identifiers.

Rebuild after each graph correction. Do not suppress a diagnostic until its documented meaning and the project's intended behavior have both been checked.
