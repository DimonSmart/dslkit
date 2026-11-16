namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlSchemaDdlGrammar
    {
        public static void BuildTableAndIndexGrammar(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;

            var expression = gb.NT("Expression");
            var contextualIdentifierTerm = gb.NT("ContextualIdentifierTerm");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var strictQualifiedName = gb.NT("StrictQualifiedName");
            var qualifiedName = gb.NT("QualifiedName");
            var searchCondition = gb.NT("SearchCondition");
            var typeSpec = gb.NT("TypeSpec");
            var namedOptionValue = gb.NT("NamedOptionValue");
            var createTableKeyColumnList = gb.NT("CreateTableKeyColumnList");
            var indexOptionList = gb.NT("IndexOptionList");
            var indexOnOffValue = gb.NT("IndexOnOffValue");
            var forSystemTimeStart = context.ForSystemTimeStartTerminal;

            var createTableFileTableClause = gb.NT("CreateTableFileTableClause");
            var createTableStatement = gb.NT("CreateTableStatement");
            var createIndexFileStreamClause = gb.NT("CreateIndexFileStreamClause");
            var createTableOptions = gb.NT("CreateTableOptions");
            var createTableElementList = gb.NT("CreateTableElementList");
            var createTableTailClauseList = gb.NT("CreateTableTailClauseList");
            var createTableTailClause = gb.NT("CreateTableTailClause");
            var createTablePeriodClause = gb.NT("CreateTablePeriodClause");
            var createTableOnClause = gb.NT("CreateTableOnClause");
            var createTableTextImageClause = gb.NT("CreateTableTextImageClause");
            var createTableElement = gb.NT("CreateTableElement");
            var createTableColumnDefinition = gb.NT("CreateTableColumnDefinition");
            var createTableComputedColumn = gb.NT("CreateTableComputedColumn");
            var createTableColumnSet = gb.NT("CreateTableColumnSet");
            var createTableConstraint = gb.NT("CreateTableConstraint");
            var createTableTableIndex = gb.NT("CreateTableTableIndex");
            var createTableColumnOptionList = gb.NT("CreateTableColumnOptionList");
            var createTableColumnOption = gb.NT("CreateTableColumnOption");
            var maskingOptionList = gb.NT("MaskingOptionList");
            var encryptionOptionList = gb.NT("EncryptionOptionList");
            var createTableColumnConstraintBody = gb.NT("CreateTableColumnConstraintBody");
            var createTableTableConstraintBody = gb.NT("CreateTableTableConstraintBody");
            var createTableColumnKeyClusterType = gb.NT("CreateTableColumnKeyClusterType");
            var createTableConstraintClusterType = gb.NT("CreateTableConstraintClusterType");
            var createTableClusterType = gb.NT("CreateTableClusterType");
            var createTableKeyColumn = gb.NT("CreateTableKeyColumn");
            var createIndexWithClause = gb.NT("CreateIndexWithClause");
            var createTableOptionList = gb.NT("CreateTableOptionList");
            var createTableOption = gb.NT("CreateTableOption");
            var createTableDurabilityMode = gb.NT("CreateTableDurabilityMode");
            var alterTableStatement = gb.NT("AlterTableStatement");
            var alterTableAction = gb.NT("AlterTableAction");
            var alterTableAddItemList = gb.NT("AlterTableAddItemList");
            var alterTableAddItem = gb.NT("AlterTableAddItem");
            var alterTableAlterColumnAction = gb.NT("AlterTableAlterColumnAction");
            var alterTableColumnOptionList = gb.NT("AlterTableColumnOptionList");
            var alterTableColumnOption = gb.NT("AlterTableColumnOption");
            var alterTableDropItemList = gb.NT("AlterTableDropItemList");
            var alterTableDropItem = gb.NT("AlterTableDropItem");
            var alterTableCheckMode = gb.NT("AlterTableCheckMode");
            var alterTableConstraintTarget = gb.NT("AlterTableConstraintTarget");
            var alterTableTriggerTarget = gb.NT("AlterTableTriggerTarget");
            var createKeyListIndexHead = gb.NT("CreateKeyListIndexHead");
            var createKeylessIndexHead = gb.NT("CreateKeylessIndexHead");
            var createIndexStatement = gb.NT("CreateIndexStatement");
            var createIndexKeyList = gb.NT("CreateIndexKeyList");
            var createIndexKeyItem = gb.NT("CreateIndexKeyItem");
            var createIndexTailClauseList = gb.NT("CreateIndexTailClauseList");
            var createIndexTailClause = gb.NT("CreateIndexTailClause");
            var createIndexIncludeClause = gb.NT("CreateIndexIncludeClause");
            var createIndexIncludeList = gb.NT("CreateIndexIncludeList");
            var createIndexWhereClause = gb.NT("CreateIndexWhereClause");
            var createIndexStorageClause = gb.NT("CreateIndexStorageClause");
            var indexStorageTarget = gb.NT("IndexStorageTarget");
            var indexFileStreamTarget = gb.NT("IndexFileStreamTarget");
            var indexOption = gb.NT("IndexOption");
            var indexOptionName = gb.NT("IndexOptionName");
            var indexOptionValue = gb.NT("IndexOptionValue");
            var indexPartitionList = gb.NT("IndexPartitionList");
            var indexPartitionItem = gb.NT("IndexPartitionItem");
            var alterIndexStatement = gb.NT("AlterIndexStatement");
            var alterIndexTarget = gb.NT("AlterIndexTarget");
            var alterIndexAction = gb.NT("AlterIndexAction");
            var alterIndexRebuildSpec = gb.NT("AlterIndexRebuildSpec");
            var alterIndexReorganizeSpec = gb.NT("AlterIndexReorganizeSpec");
            var alterIndexResumeSpec = gb.NT("AlterIndexResumeSpec");
            var alterIndexPartitionSelector = gb.NT("AlterIndexPartitionSelector");
            var createTablePeriodClauseOpt = gb.NT("CreateTablePeriodClauseOpt");
            var createTableOptionsOpt = gb.NT("CreateTableOptionsOpt");
            var createTableOnClauseOpt = gb.NT("CreateTableOnClauseOpt");
            var createTableTextImageClauseOpt = gb.NT("CreateTableTextImageClauseOpt");
            var createTableFileStreamClauseOpt = gb.NT("CreateTableFileStreamClauseOpt");
            var createIndexIncludeClauseOpt = gb.NT("CreateIndexIncludeClauseOpt");
            var createIndexWhereClauseOpt = gb.NT("CreateIndexWhereClauseOpt");
            var createIndexWithClauseOpt = gb.NT("CreateIndexWithClauseOpt");
            var createIndexStorageClauseOpt = gb.NT("CreateIndexStorageClauseOpt");
            var createIndexFileStreamClauseOpt = gb.NT("CreateIndexFileStreamClauseOpt");
            var schemaDdlIdentifierTerm = gb.NT("SchemaDdlIdentifierTerm");
            var schemaDdlIdentifierList = gb.NT("SchemaDdlIdentifierList");

            gb.Rule(schemaDdlIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME");
            gb.Rule(schemaDdlIdentifierList).SeparatedBy(",", schemaDdlIdentifierTerm);

            gb.Prod(createTableFileTableClause).Is("AS", "FILETABLE");
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")");
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createIndexFileStreamClause);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createTableOptions);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createIndexFileStreamClause, createTableOptions);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList);
            if (context.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", "AS", "EDGE");
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", "AS", "NODE");
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, "AS", "EDGE");
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, "AS", "NODE");
            }

            gb.Prod(createTableTailClauseList).Is(
                createTablePeriodClause,
                createTableOptionsOpt,
                createTableOnClauseOpt,
                createTableTextImageClauseOpt,
                createTableFileStreamClauseOpt);
            gb.Prod(createTableTailClauseList).Is(
                createTableOptions,
                createTableOnClauseOpt,
                createTableTextImageClauseOpt,
                createTableFileStreamClauseOpt);
            gb.Prod(createTableTailClauseList).Is(
                createTableOnClause,
                createTableTextImageClauseOpt,
                createTableFileStreamClauseOpt);
            gb.Prod(createTableTailClauseList).Is(createTableTextImageClause, createTableFileStreamClauseOpt);
            gb.Prod(createTableTailClauseList).Is(createIndexFileStreamClause);
            gb.Opt(createTablePeriodClauseOpt, createTablePeriodClause);
            gb.Opt(createTableOptionsOpt, createTableOptions);
            gb.Opt(createTableOnClauseOpt, createTableOnClause);
            gb.Opt(createTableTextImageClauseOpt, createTableTextImageClause);
            gb.Opt(createTableFileStreamClauseOpt, createIndexFileStreamClause);

            gb.Rule(createTableElementList).SeparatedBy(",", createTableElement);
            gb.Prod(createTableElement).Is(createTableColumnDefinition);
            gb.Prod(createTableElement).Is(createTableComputedColumn);
            gb.Prod(createTableElement).Is(createTableColumnSet);
            gb.Prod(createTableElement).Is(createTableConstraint);
            gb.Prod(createTableElement).Is(createTableTableIndex);

            gb.Prod(createTableColumnDefinition).Is(schemaDdlIdentifierTerm, typeSpec);
            gb.Prod(createTableColumnDefinition).Is(schemaDdlIdentifierTerm, typeSpec, createTableColumnOptionList);
            gb.Rule(createTableColumnOptionList).Plus(createTableColumnOption);
            gb.Prod(createTableColumnOption).Is("NULL");
            gb.Prod(createTableColumnOption).Is("NOT", "NULL");
            gb.Prod(createTableColumnOption).Is("PRIMARY", "KEY");
            gb.Rule(createTableColumnOption).Keywords("UNIQUE", "SPARSE", "PERSISTED", "ROWGUIDCOL", "COLUMN_SET");
            gb.Prod(createTableColumnOption).Is("FOR", "ALL_SPARSE_COLUMNS");
            gb.Prod(createTableColumnOption).Is("FILESTREAM");
            gb.Prod(createTableColumnOption).Is("DEFAULT", expression);
            gb.Prod(createTableColumnOption).Is("IDENTITY");
            gb.Prod(createTableColumnOption).Is("IDENTITY", "(", expression, ",", expression, ")");
            gb.Prod(createTableColumnOption).Is("COLLATE", strictIdentifierTerm);
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "START");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "START", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "END");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "END", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "START");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "START", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "END");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "END", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "START");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "START", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "END");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "END", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("MASKED", "WITH", "(", maskingOptionList, ")");
            gb.Prod(createTableColumnOption).Is("ENCRYPTED", "WITH", "(", encryptionOptionList, ")");
            gb.Prod(createTableColumnOption).Is("NOT", "FOR", "REPLICATION");
            gb.Prod(createTableColumnOption).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableColumnOption).Is("REFERENCES", qualifiedName);
            gb.Prod(createTableColumnOption).Is("REFERENCES", qualifiedName, "(", schemaDdlIdentifierList, ")");
            gb.Prod(createTableColumnOption).Is("CONSTRAINT", schemaDdlIdentifierTerm, createTableColumnConstraintBody);

            gb.Prod(createTableComputedColumn).Is(schemaDdlIdentifierTerm, "AS", expression);
            gb.Prod(createTableComputedColumn).Is(schemaDdlIdentifierTerm, "AS", expression, createTableColumnOptionList);

            gb.Prod(createTableColumnSet).Is(schemaDdlIdentifierTerm, typeSpec, "COLUMN_SET", "FOR", "ALL_SPARSE_COLUMNS");

            gb.Prod(createTableConstraint).Is(createTableTableConstraintBody);
            gb.Prod(createTableConstraint).Is("CONSTRAINT", schemaDdlIdentifierTerm, createTableTableConstraintBody);

            gb.Prod(createTableColumnConstraintBody).Is("PRIMARY", "KEY");
            gb.Prod(createTableColumnConstraintBody).Is("PRIMARY", "KEY", createTableColumnKeyClusterType);
            gb.Prod(createTableColumnConstraintBody).Is("UNIQUE");
            gb.Prod(createTableColumnConstraintBody).Is("UNIQUE", createTableColumnKeyClusterType);
            gb.Prod(createTableColumnConstraintBody).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableColumnConstraintBody).Is("FOREIGN", "KEY", "REFERENCES", qualifiedName);
            gb.Prod(createTableColumnConstraintBody).Is("FOREIGN", "KEY", "REFERENCES", qualifiedName, "(", schemaDdlIdentifierList, ")");
            gb.Prod(createTableColumnConstraintBody).Is("REFERENCES", qualifiedName);
            gb.Prod(createTableColumnConstraintBody).Is("REFERENCES", qualifiedName, "(", schemaDdlIdentifierList, ")");
            gb.Prod(createTableColumnConstraintBody).Is("DEFAULT", expression);

            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "ON", indexStorageTarget);
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod(createTableTableConstraintBody).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableTableConstraintBody).Is("FOREIGN", "KEY", "(", schemaDdlIdentifierList, ")", "REFERENCES", qualifiedName);
            gb.Prod(createTableTableConstraintBody).Is("FOREIGN", "KEY", "(", schemaDdlIdentifierList, ")", "REFERENCES", qualifiedName, "(", schemaDdlIdentifierList, ")");
            gb.Prod(createTableTableConstraintBody).Is("DEFAULT", expression, "FOR", schemaDdlIdentifierTerm);
            gb.Prod(createTableTableConstraintBody).Is("DEFAULT", "(", expression, ")", "FOR", schemaDdlIdentifierTerm);

            gb.Rule(createTableColumnKeyClusterType)
                .CanBe("CLUSTERED")
                .Or("NONCLUSTERED");

            gb.Rule(createTableConstraintClusterType)
                .CanBe("NONCLUSTERED", "HASH")
                .Or("CLUSTERED")
                .Or("NONCLUSTERED");

            gb.Rule(createTableClusterType)
                .CanBe("NONCLUSTERED", "HASH")
                .Or("CLUSTERED", "COLUMNSTORE")
                .Or("NONCLUSTERED", "COLUMNSTORE")
                .Or("CLUSTERED")
                .Or("NONCLUSTERED");

            gb.Rule(createTableKeyColumnList).SeparatedBy(",", createTableKeyColumn);
            gb.Prod(createTableKeyColumn).Is(schemaDdlIdentifierTerm);
            gb.Prod(createTableKeyColumn).Is(schemaDdlIdentifierTerm, "ASC");
            gb.Prod(createTableKeyColumn).Is(schemaDdlIdentifierTerm, "DESC");

            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, "CLUSTERED", "COLUMNSTORE");
            gb.Prod(createTableTableIndex).Is("INDEX", schemaDdlIdentifierTerm, "CLUSTERED", "COLUMNSTORE", createIndexWithClause);

            gb.Prod(createTablePeriodClause).Is("PERIOD", forSystemTimeStart, "SYSTEM_TIME", "(", schemaDdlIdentifierTerm, ",", schemaDdlIdentifierTerm, ")");
            gb.Prod(createTableOptions).Is("WITH", "(", createTableOptionList, ")");
            gb.Rule(createTableOptionList).SeparatedBy(",", createTableOption);
            gb.Prod(createTableOption).Is("MEMORY_OPTIMIZED", "=", indexOnOffValue);
            gb.Prod(createTableOption).Is("DURABILITY", "=", createTableDurabilityMode);
            gb.Prod(createTableOption).Is("FILETABLE_DIRECTORY", "=", namedOptionValue);
            gb.Prod(createTableOption).Is("FILETABLE_COLLATE_FILENAME", "=", namedOptionValue);
            gb.Prod(createTableOption).Is("CLUSTERED", "COLUMNSTORE", "INDEX");
            gb.Rule(createTableDurabilityMode).Keywords("SCHEMA_AND_DATA", "SCHEMA_ONLY");
            gb.Prod(createTableOnClause).Is("ON", indexStorageTarget);
            gb.Prod(createTableTextImageClause).Is("TEXTIMAGE_ON", indexStorageTarget);

            gb.Prod(alterTableStatement).Is("ALTER", "TABLE", qualifiedName, alterTableAction);
            gb.Prod(alterTableAction).Is("ADD", alterTableAddItemList);
            gb.Prod(alterTableAction).Is("ALTER", "COLUMN", alterTableAlterColumnAction);
            gb.Prod(alterTableAction).Is("DROP", alterTableDropItemList);
            gb.Prod(alterTableAction).Is("WITH", alterTableCheckMode, "ADD", createTableConstraint);
            gb.Prod(alterTableAction).Is(alterTableCheckMode, "CONSTRAINT", alterTableConstraintTarget);
            gb.Prod(alterTableAction).Is("ENABLE", "TRIGGER", alterTableTriggerTarget);
            gb.Prod(alterTableAction).Is("DISABLE", "TRIGGER", alterTableTriggerTarget);
            gb.Prod(alterTableAction).Is("ADD", createTablePeriodClause);
            gb.Prod(alterTableAction).Is("DROP", "PERIOD", forSystemTimeStart, "SYSTEM_TIME");
            gb.Prod(alterTableAction).Is("SET", "(", indexOptionList, ")");
            gb.Rule(alterTableAddItemList).SeparatedBy(",", alterTableAddItem);
            gb.Prod(alterTableAddItem).Is(createTableColumnDefinition);
            gb.Prod(alterTableAddItem).Is(createTableConstraint);
            gb.Prod(alterTableAddItem).Is(createTableTableIndex);
            gb.Prod(alterTableAddItem).Is(createTableComputedColumn);
            gb.Prod(alterTableAlterColumnAction).Is(schemaDdlIdentifierTerm, typeSpec);
            gb.Prod(alterTableAlterColumnAction).Is(schemaDdlIdentifierTerm, typeSpec, alterTableColumnOptionList);
            gb.Prod(alterTableAlterColumnAction).Is(schemaDdlIdentifierTerm, "ADD", alterTableColumnOptionList);
            gb.Prod(alterTableAlterColumnAction).Is(schemaDdlIdentifierTerm, "DROP", alterTableColumnOptionList);
            gb.Rule(alterTableColumnOptionList).Plus(alterTableColumnOption);
            gb.Prod(alterTableColumnOption).Is(createTableColumnOption);
            gb.Rule(alterTableDropItemList).SeparatedBy(",", alterTableDropItem);
            gb.Prod(alterTableDropItem).Is("COLUMN", schemaDdlIdentifierTerm);
            gb.Prod(alterTableDropItem).Is("CONSTRAINT", schemaDdlIdentifierTerm);
            gb.Prod(alterTableDropItem).Is("CONSTRAINT", "ALL");
            gb.Rule(alterTableCheckMode).Keywords("CHECK", "NOCHECK");
            gb.Rule(alterTableConstraintTarget)
                .CanBe(schemaDdlIdentifierTerm)
                .OrKeywords("ALL");
            gb.Rule(alterTableTriggerTarget)
                .CanBe(schemaDdlIdentifierTerm)
                .OrKeywords("ALL");

            gb.Prod(createKeyListIndexHead).Is("CREATE", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "CLUSTERED", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "NONCLUSTERED", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "CLUSTERED", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "NONCLUSTERED", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "NONCLUSTERED", "COLUMNSTORE", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createKeylessIndexHead).Is("CREATE", "CLUSTERED", "COLUMNSTORE", "INDEX", schemaDdlIdentifierTerm);
            gb.Prod(createIndexStatement).Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")");
            gb.Prod(createIndexStatement).Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")", createIndexTailClauseList);
            gb.Prod(createIndexStatement).Is(createKeylessIndexHead, "ON", qualifiedName);
            gb.Prod(createIndexStatement).Is(createKeylessIndexHead, "ON", qualifiedName, createIndexTailClauseList);

            gb.Rule(createIndexKeyList).SeparatedBy(",", createIndexKeyItem);
            gb.Prod(createIndexKeyItem).Is(schemaDdlIdentifierTerm);
            gb.Prod(createIndexKeyItem).Is(schemaDdlIdentifierTerm, "ASC");
            gb.Prod(createIndexKeyItem).Is(schemaDdlIdentifierTerm, "DESC");

            gb.Prod(createIndexTailClauseList).Is(
                createIndexIncludeClause,
                createIndexWhereClauseOpt,
                createIndexWithClauseOpt,
                createIndexStorageClauseOpt,
                createIndexFileStreamClauseOpt);
            gb.Prod(createIndexTailClauseList).Is(
                createIndexWhereClause,
                createIndexWithClauseOpt,
                createIndexStorageClauseOpt,
                createIndexFileStreamClauseOpt);
            gb.Prod(createIndexTailClauseList).Is(
                createIndexWithClause,
                createIndexStorageClauseOpt,
                createIndexFileStreamClauseOpt);
            gb.Prod(createIndexTailClauseList).Is(createIndexStorageClause, createIndexFileStreamClauseOpt);
            gb.Prod(createIndexTailClauseList).Is(createIndexFileStreamClause);
            gb.Opt(createIndexIncludeClauseOpt, createIndexIncludeClause);
            gb.Opt(createIndexWhereClauseOpt, createIndexWhereClause);
            gb.Opt(createIndexWithClauseOpt, createIndexWithClause);
            gb.Opt(createIndexStorageClauseOpt, createIndexStorageClause);
            gb.Opt(createIndexFileStreamClauseOpt, createIndexFileStreamClause);

            gb.Prod(createIndexIncludeClause).Is("INCLUDE", "(", createIndexIncludeList, ")");
            gb.Rule(createIndexIncludeList).SeparatedBy(",", schemaDdlIdentifierTerm);
            gb.Prod(createIndexWhereClause).Is("WHERE", searchCondition);
            gb.Prod(createIndexWithClause).Is("WITH", "(", indexOptionList, ")");
            gb.Prod(createIndexStorageClause).Is("ON", indexStorageTarget);
            gb.Prod(createIndexFileStreamClause).Is("FILESTREAM_ON", indexFileStreamTarget);

            gb.Prod(indexStorageTarget).Is(qualifiedName);
            gb.Prod(indexStorageTarget).Is(qualifiedName, "(", schemaDdlIdentifierTerm, ")");
            gb.Prod(indexFileStreamTarget).Is(qualifiedName);
            gb.Prod(indexFileStreamTarget).Is("NULL");
            gb.Prod(indexFileStreamTarget).Is(qualifiedName, "(", schemaDdlIdentifierTerm, ")");

            gb.Rule(indexOptionList).SeparatedBy(",", indexOption);
            gb.Prod(indexOptionList).Is(indexOptionList, ",");
            gb.Prod(indexOption).Is(indexOptionName, "=", indexOptionValue);
            gb.Prod(indexOption).Is(indexOptionName, "(", indexOptionList, ")");
            gb.Prod(indexOption).Is(indexOptionName, "=", indexOptionValue, "ON", "PARTITIONS", "(", indexPartitionList, ")");

            gb.Rule(indexOptionName)
                .CanBe("PAD_INDEX")
                .Or("FILLFACTOR")
                .Or("SORT_IN_TEMPDB")
                .Or("IGNORE_DUP_KEY")
                .Or("STATISTICS_NORECOMPUTE")
                .Or("STATISTICS_INCREMENTAL")
                .Or("DROP_EXISTING")
                .Or("ONLINE")
                .Or("ALLOW_ROW_LOCKS")
                .Or("ALLOW_PAGE_LOCKS")
                .Or("OPTIMIZE_FOR_SEQUENTIAL_KEY")
                .Or("MAXDOP")
                .Or("MAX_DURATION")
                .Or("DATA_COMPRESSION")
                .Or("XML_COMPRESSION")
                .Or("COMPRESSION_DELAY")
                .Or("WAIT_AT_LOW_PRIORITY")
                .Or("ABORT_AFTER_WAIT")
                .Or("BUCKET_COUNT")
                .Or("COMPRESS_ALL_ROW_GROUPS")
                .Or("LOB_COMPACTION")
                .Or("RESUMABLE");

            gb.Prod(indexOptionValue).Is(expression);
            gb.Prod(indexOptionValue).Is(indexOnOffValue);
            gb.Rule(indexOptionValue).Keywords("NONE", "SELF", "BLOCKERS", "ROW", "COLUMNSTORE_ARCHIVE");
            gb.Prod(indexOptionValue).Is(expression, "MINUTES");
            gb.Prod(indexOptionValue).Is(indexOnOffValue, "(", indexOptionList, ")");
            gb.Prod(indexOptionValue).Is("(", indexOptionList, ")");
            gb.Rule(indexOnOffValue).Keywords("ON", "OFF");

            gb.Prod(namedOptionValue).Is(expression);
            gb.Rule(namedOptionValue).Keywords("ON", "OFF");

            gb.Prod(maskingOptionList).Is("FUNCTION", "=", expression);

            gb.Prod(encryptionOptionList).Is("COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is(encryptionOptionList, ",", "ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is(encryptionOptionList, ",", "ALGORITHM", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is(encryptionOptionList, ",", "COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is("ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is("ALGORITHM", "=", namedOptionValue);

            gb.Rule(indexPartitionList).SeparatedBy(",", indexPartitionItem);
            gb.Prod(indexPartitionItem).Is(expression);
            gb.Prod(indexPartitionItem).Is(expression, "TO", expression);

            gb.Prod(alterIndexStatement).Is("ALTER", "INDEX", alterIndexTarget, "ON", qualifiedName, alterIndexAction);
            gb.Rule(alterIndexTarget)
                .CanBe(schemaDdlIdentifierTerm)
                .Or(strictQualifiedName)
                .OrKeywords("ALL");
            gb.Rule(alterIndexAction)
                .CanBe("REBUILD")
                .Or("REBUILD", alterIndexRebuildSpec)
                .Or("DISABLE")
                .Or("REORGANIZE")
                .Or("REORGANIZE", alterIndexReorganizeSpec)
                .Or("SET", "(", indexOptionList, ")")
                .Or("RESUME")
                .Or("RESUME", alterIndexResumeSpec)
                .Or("PAUSE")
                .Or("ABORT");

            gb.Prod(alterIndexRebuildSpec).Is("WITH", "(", indexOptionList, ")");
            gb.Prod(alterIndexRebuildSpec).Is("PARTITION", "=", alterIndexPartitionSelector);
            gb.Prod(alterIndexRebuildSpec).Is("PARTITION", "=", alterIndexPartitionSelector, "WITH", "(", indexOptionList, ")");
            gb.Prod(alterIndexReorganizeSpec).Is("PARTITION", "=", alterIndexPartitionSelector);
            gb.Prod(alterIndexReorganizeSpec).Is("WITH", "(", indexOptionList, ")");
            gb.Prod(alterIndexReorganizeSpec).Is("PARTITION", "=", alterIndexPartitionSelector, "WITH", "(", indexOptionList, ")");
            gb.Prod(alterIndexResumeSpec).Is("WITH", "(", indexOptionList, ")");
            gb.Prod(alterIndexPartitionSelector).Is(expression);
        }

        public static void BuildDatabaseGrammar(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;

            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;

            var createDatabaseStatement = gb.NT("CreateDatabaseStatement");
            var createDatabaseClauseList = gb.NT("CreateDatabaseClauseList");
            var createDatabaseClause = gb.NT("CreateDatabaseClause");
            var createDatabaseContainmentClause = gb.NT("CreateDatabaseContainmentClause");
            var createDatabaseOnClause = gb.NT("CreateDatabaseOnClause");
            var createDatabaseOnFilespecSequence = gb.NT("CreateDatabaseOnFilespecSequence");
            var createDatabaseFilespec = gb.NT("CreateDatabaseFilespec");
            var createDatabaseFilegroup = gb.NT("CreateDatabaseFilegroup");
            var createDatabaseFilespecList = gb.NT("CreateDatabaseFilespecList");
            var createDatabaseCollateClause = gb.NT("CreateDatabaseCollateClause");
            var createDatabaseWithClause = gb.NT("CreateDatabaseWithClause");
            var createDatabaseOptionList = gb.NT("CreateDatabaseOptionList");
            var createDatabaseOption = gb.NT("CreateDatabaseOption");
            var createDatabaseOptionValue = gb.NT("CreateDatabaseOptionValue");
            var createDatabaseOnOffValue = gb.NT("CreateDatabaseOnOffValue");
            var createDatabaseFilestreamOptionList = gb.NT("CreateDatabaseFilestreamOptionList");
            var createDatabaseFilestreamOption = gb.NT("CreateDatabaseFilestreamOption");
            var createDatabaseNonTransactedAccessValue = gb.NT("CreateDatabaseNonTransactedAccessValue");
            var createDatabaseFileName = gb.NT("CreateDatabaseFileName");
            var createDatabaseFilespecOptionList = gb.NT("CreateDatabaseFilespecOptionList");
            var createDatabaseFilespecOption = gb.NT("CreateDatabaseFilespecOption");
            var createDatabaseSizeSpec = gb.NT("CreateDatabaseSizeSpec");
            var createDatabaseMaxSizeSpec = gb.NT("CreateDatabaseMaxSizeSpec");
            var createDatabaseGrowthSpec = gb.NT("CreateDatabaseGrowthSpec");
            var createDatabaseSizeUnit = gb.NT("CreateDatabaseSizeUnit");
            var createDatabaseGrowthUnit = gb.NT("CreateDatabaseGrowthUnit");
            var createDatabaseOnClauseOpt = gb.NT("CreateDatabaseOnClauseOpt");
            var createDatabaseCollateClauseOpt = gb.NT("CreateDatabaseCollateClauseOpt");
            var createDatabaseWithClauseOpt = gb.NT("CreateDatabaseWithClauseOpt");

            gb.Prod(createDatabaseStatement).Is("CREATE", "DATABASE", strictIdentifierTerm);
            gb.Prod(createDatabaseStatement).Is("CREATE", "DATABASE", strictIdentifierTerm, createDatabaseClauseList);
            gb.Prod(createDatabaseClauseList).Is(
                createDatabaseContainmentClause,
                createDatabaseOnClauseOpt,
                createDatabaseCollateClauseOpt,
                createDatabaseWithClauseOpt);
            gb.Prod(createDatabaseClauseList).Is(
                createDatabaseOnClause,
                createDatabaseCollateClauseOpt,
                createDatabaseWithClauseOpt);
            gb.Prod(createDatabaseClauseList).Is(createDatabaseCollateClause, createDatabaseWithClauseOpt);
            gb.Prod(createDatabaseClauseList).Is(createDatabaseWithClause);
            gb.Opt(createDatabaseOnClauseOpt, createDatabaseOnClause);
            gb.Opt(createDatabaseCollateClauseOpt, createDatabaseCollateClause);
            gb.Opt(createDatabaseWithClauseOpt, createDatabaseWithClause);

            gb.Prod(createDatabaseContainmentClause).Is("CONTAINMENT", "=", "NONE");
            gb.Prod(createDatabaseContainmentClause).Is("CONTAINMENT", "=", "PARTIAL");

            gb.Prod(createDatabaseOnClause).Is("ON", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseOnClause).Is("ON", "PRIMARY", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseOnFilespecSequence).Is(createDatabaseFilespec);
            gb.Prod(createDatabaseOnFilespecSequence).Is(createDatabaseFilespec, ",", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseOnFilespecSequence).Is(createDatabaseFilespec, ",", createDatabaseFilegroup);
            gb.Prod(createDatabaseOnFilespecSequence).Is(createDatabaseFilespec, ",", "LOG", "ON", createDatabaseFilespecList);

            gb.Rule(createDatabaseFilespecList).SeparatedBy(",", createDatabaseFilespec);

            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", strictIdentifierTerm, createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", strictIdentifierTerm, "DEFAULT", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", strictIdentifierTerm, "CONTAINS", "FILESTREAM", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", strictIdentifierTerm, "CONTAINS", "FILESTREAM", "DEFAULT", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", strictIdentifierTerm, "CONTAINS", "MEMORY_OPTIMIZED_DATA", createDatabaseOnFilespecSequence);

            gb.Prod(createDatabaseCollateClause).Is("COLLATE", strictIdentifierTerm);

            gb.Prod(createDatabaseWithClause).Is("WITH", createDatabaseOptionList);
            gb.Rule(createDatabaseOptionList).SeparatedBy(",", createDatabaseOption);

            gb.Prod(createDatabaseOption).Is("FILESTREAM", "(", createDatabaseFilestreamOptionList, ")");
            gb.Prod(createDatabaseOption).Is("DEFAULT_FULLTEXT_LANGUAGE", "=", createDatabaseOptionValue);
            gb.Prod(createDatabaseOption).Is("DEFAULT_LANGUAGE", "=", createDatabaseOptionValue);
            gb.Prod(createDatabaseOption).Is("NESTED_TRIGGERS", "=", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is("TRANSFORM_NOISE_WORDS", "=", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is("TWO_DIGIT_YEAR_CUTOFF", "=", number);
            gb.Prod(createDatabaseOption).Is("DB_CHAINING", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is("DB_CHAINING", "=", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is("TRUSTWORTHY", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is("TRUSTWORTHY", "=", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is("LEDGER", "=", createDatabaseOnOffValue);
            gb.Prod(createDatabaseOption).Is(
                "PERSISTENT_LOG_BUFFER",
                "=",
                "ON",
                "(",
                "DIRECTORY_NAME",
                "=",
                stringLiteral,
                ")");

            gb.Prod(createDatabaseOptionValue).Is(number);
            gb.Prod(createDatabaseOptionValue).Is(strictIdentifierTerm);
            gb.Prod(createDatabaseOptionValue).Is(stringLiteral);

            gb.Rule(createDatabaseOnOffValue).Keywords("ON", "OFF");

            gb.Rule(createDatabaseFilestreamOptionList).SeparatedBy(",", createDatabaseFilestreamOption);
            gb.Prod(createDatabaseFilestreamOption).Is("NON_TRANSACTED_ACCESS", "=", createDatabaseNonTransactedAccessValue);
            gb.Prod(createDatabaseFilestreamOption).Is("DIRECTORY_NAME", "=", stringLiteral);
            gb.Rule(createDatabaseNonTransactedAccessValue).Keywords("OFF", "READ_ONLY", "FULL");

            gb.Prod(createDatabaseFilespec).Is(
                "(",
                "NAME",
                "=",
                createDatabaseFileName,
                ",",
                "FILENAME",
                "=",
                stringLiteral,
                ")");
            gb.Prod(createDatabaseFilespec).Is(
                "(",
                "NAME",
                "=",
                createDatabaseFileName,
                ",",
                "FILENAME",
                "=",
                stringLiteral,
                ",",
                createDatabaseFilespecOptionList,
                ")");

            gb.Prod(createDatabaseFileName).Is(strictIdentifierTerm);
            gb.Prod(createDatabaseFileName).Is(stringLiteral);

            gb.Rule(createDatabaseFilespecOptionList).SeparatedBy(",", createDatabaseFilespecOption);
            gb.Prod(createDatabaseFilespecOption).Is("SIZE", "=", createDatabaseSizeSpec);
            gb.Prod(createDatabaseFilespecOption).Is("MAXSIZE", "=", createDatabaseMaxSizeSpec);
            gb.Prod(createDatabaseFilespecOption).Is("FILEGROWTH", "=", createDatabaseGrowthSpec);

            gb.Prod(createDatabaseSizeSpec).Is(number);
            gb.Prod(createDatabaseSizeSpec).Is(number, createDatabaseSizeUnit);
            gb.Prod(createDatabaseSizeSpec).Is(number, strictIdentifierTerm);

            gb.Prod(createDatabaseMaxSizeSpec).Is("UNLIMITED");
            gb.Prod(createDatabaseMaxSizeSpec).Is(number);
            gb.Prod(createDatabaseMaxSizeSpec).Is(number, createDatabaseSizeUnit);
            gb.Prod(createDatabaseMaxSizeSpec).Is(number, strictIdentifierTerm);

            gb.Prod(createDatabaseGrowthSpec).Is(number);
            gb.Prod(createDatabaseGrowthSpec).Is(number, createDatabaseGrowthUnit);
            gb.Prod(createDatabaseGrowthSpec).Is(number, strictIdentifierTerm);

            gb.Rule(createDatabaseSizeUnit).Keywords("KB", "MB", "GB", "TB");

            gb.Prod(createDatabaseGrowthUnit).Is(createDatabaseSizeUnit);
            gb.Prod(createDatabaseGrowthUnit).Is("%");
        }
    }
}
