// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress SQL validation warnings for PostgreSQL scripts
[assembly: SuppressMessage("SQL", "SQL80001", Justification = "PostgreSQL syntax - not SQL Server")]
[assembly: SuppressMessage("SQL", "SQL71502", Justification = "PostgreSQL syntax - not SQL Server")]
[assembly: SuppressMessage("SQL", "SQL71501", Justification = "PostgreSQL syntax - not SQL Server")]
