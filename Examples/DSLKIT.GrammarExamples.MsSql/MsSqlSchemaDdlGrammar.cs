namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlSchemaDdlGrammar
    {
        public static void BuildTableAndIndexGrammar(MsSqlGrammarContext context, MsSqlSchemaDdlSymbols symbols)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var identifierList = context.Symbols.IdentifierList;
            var identifierTerm = context.Symbols.IdentifierTerm;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var qualifiedName = context.Symbols.QualifiedName;
            var searchCondition = context.Symbols.SearchCondition;
            var typeSpec = context.Symbols.TypeSpec;
            var namedOptionValue = context.Symbols.NamedOptionValue;
            var createTableKeyColumnList = context.Symbols.CreateTableKeyColumnList;
            var indexOptionList = context.Symbols.IndexOptionList;
            var indexOnOffValue = context.Symbols.IndexOnOffValue;
            var forSystemTimeStart = context.ForSystemTimeStartTerminal;

            var createTableFileTableClause = symbols.CreateTableFileTableClause;
            var createTableStatement = symbols.CreateTableStatement;
            var createIndexFileStreamClause = symbols.CreateIndexFileStreamClause;
            var createTableOptions = symbols.CreateTableOptions;
            var createTableElementList = symbols.CreateTableElementList;
            var createTableTailClauseList = symbols.CreateTableTailClauseList;
            var createTableTailClause = symbols.CreateTableTailClause;
            var createTablePeriodClause = symbols.CreateTablePeriodClause;
            var createTableOnClause = symbols.CreateTableOnClause;
            var createTableTextImageClause = symbols.CreateTableTextImageClause;
            var createTableElement = symbols.CreateTableElement;
            var createTableColumnDefinition = symbols.CreateTableColumnDefinition;
            var createTableComputedColumn = symbols.CreateTableComputedColumn;
            var createTableColumnSet = symbols.CreateTableColumnSet;
            var createTableConstraint = symbols.CreateTableConstraint;
            var createTableTableIndex = symbols.CreateTableTableIndex;
            var createTableColumnOptionList = symbols.CreateTableColumnOptionList;
            var createTableColumnOption = symbols.CreateTableColumnOption;
            var maskingOptionList = symbols.MaskingOptionList;
            var encryptionOptionList = symbols.EncryptionOptionList;
            var createTableColumnConstraintBody = symbols.CreateTableColumnConstraintBody;
            var createTableTableConstraintBody = symbols.CreateTableTableConstraintBody;
            var createTableColumnKeyClusterType = symbols.CreateTableColumnKeyClusterType;
            var createTableConstraintClusterType = symbols.CreateTableConstraintClusterType;
            var createTableClusterType = symbols.CreateTableClusterType;
            var createTableKeyColumn = symbols.CreateTableKeyColumn;
            var createIndexWithClause = symbols.CreateIndexWithClause;
            var createTableOptionList = symbols.CreateTableOptionList;
            var createTableOption = symbols.CreateTableOption;
            var createTableDurabilityMode = symbols.CreateTableDurabilityMode;
            var alterTableStatement = symbols.AlterTableStatement;
            var alterTableAction = symbols.AlterTableAction;
            var alterTableAddItemList = symbols.AlterTableAddItemList;
            var alterTableAddItem = symbols.AlterTableAddItem;
            var alterTableAlterColumnAction = symbols.AlterTableAlterColumnAction;
            var alterTableColumnOptionList = symbols.AlterTableColumnOptionList;
            var alterTableColumnOption = symbols.AlterTableColumnOption;
            var alterTableDropItemList = symbols.AlterTableDropItemList;
            var alterTableDropItem = symbols.AlterTableDropItem;
            var alterTableCheckMode = symbols.AlterTableCheckMode;
            var alterTableConstraintTarget = symbols.AlterTableConstraintTarget;
            var alterTableTriggerTarget = symbols.AlterTableTriggerTarget;
            var createKeyListIndexHead = symbols.CreateKeyListIndexHead;
            var createKeylessIndexHead = symbols.CreateKeylessIndexHead;
            var createIndexStatement = symbols.CreateIndexStatement;
            var createIndexKeyList = symbols.CreateIndexKeyList;
            var createIndexKeyItem = symbols.CreateIndexKeyItem;
            var createIndexTailClauseList = symbols.CreateIndexTailClauseList;
            var createIndexTailClause = symbols.CreateIndexTailClause;
            var createIndexIncludeClause = symbols.CreateIndexIncludeClause;
            var createIndexIncludeList = symbols.CreateIndexIncludeList;
            var createIndexWhereClause = symbols.CreateIndexWhereClause;
            var createIndexStorageClause = symbols.CreateIndexStorageClause;
            var indexStorageTarget = symbols.IndexStorageTarget;
            var indexFileStreamTarget = symbols.IndexFileStreamTarget;
            var indexOption = symbols.IndexOption;
            var indexOptionName = symbols.IndexOptionName;
            var indexOptionValue = symbols.IndexOptionValue;
            var indexPartitionList = symbols.IndexPartitionList;
            var indexPartitionItem = symbols.IndexPartitionItem;
            var alterIndexStatement = symbols.AlterIndexStatement;
            var alterIndexTarget = symbols.AlterIndexTarget;
            var alterIndexAction = symbols.AlterIndexAction;
            var alterIndexRebuildSpec = symbols.AlterIndexRebuildSpec;
            var alterIndexReorganizeSpec = symbols.AlterIndexReorganizeSpec;
            var alterIndexResumeSpec = symbols.AlterIndexResumeSpec;
            var alterIndexPartitionSelector = symbols.AlterIndexPartitionSelector;
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

            gb.Prod(createTableElementList).Is(createTableElement);
            gb.Prod(createTableElementList).Is(createTableElementList, ",", createTableElement);
            gb.Prod(createTableElement).Is(createTableColumnDefinition);
            gb.Prod(createTableElement).Is(createTableComputedColumn);
            gb.Prod(createTableElement).Is(createTableColumnSet);
            gb.Prod(createTableElement).Is(createTableConstraint);
            gb.Prod(createTableElement).Is(createTableTableIndex);

            gb.Prod(createTableColumnDefinition).Is(identifierTerm, typeSpec);
            gb.Prod(createTableColumnDefinition).Is(identifierTerm, typeSpec, createTableColumnOptionList);
            gb.Prod(createTableColumnOptionList).Is(createTableColumnOption);
            gb.Prod(createTableColumnOptionList).Is(createTableColumnOptionList, createTableColumnOption);
            gb.Prod(createTableColumnOption).Is("NULL");
            gb.Prod(createTableColumnOption).Is("NOT", "NULL");
            gb.Prod(createTableColumnOption).Is("PRIMARY", "KEY");
            gb.Prod(createTableColumnOption).Is("UNIQUE");
            gb.Prod(createTableColumnOption).Is("SPARSE");
            gb.Prod(createTableColumnOption).Is("PERSISTED");
            gb.Prod(createTableColumnOption).Is("ROWGUIDCOL");
            gb.Prod(createTableColumnOption).Is("COLUMN_SET");
            gb.Prod(createTableColumnOption).Is("FOR", "ALL_SPARSE_COLUMNS");
            gb.Prod(createTableColumnOption).Is("FILESTREAM");
            gb.Prod(createTableColumnOption).Is("DEFAULT", expression);
            gb.Prod(createTableColumnOption).Is("DEFAULT", "(", expression, ")");
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
            gb.Prod(createTableColumnOption).Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableColumnOption).Is("CONSTRAINT", strictIdentifierTerm, createTableColumnConstraintBody);

            gb.Prod(createTableComputedColumn).Is(identifierTerm, "AS", expression);
            gb.Prod(createTableComputedColumn).Is(identifierTerm, "AS", expression, createTableColumnOptionList);

            gb.Prod(createTableColumnSet).Is(identifierTerm, typeSpec, "COLUMN_SET", "FOR", "ALL_SPARSE_COLUMNS");

            gb.Prod(createTableConstraint).Is(createTableTableConstraintBody);
            gb.Prod(createTableConstraint).Is("CONSTRAINT", identifierTerm, createTableTableConstraintBody);

            gb.Prod(createTableColumnConstraintBody).Is("PRIMARY", "KEY");
            gb.Prod(createTableColumnConstraintBody).Is("PRIMARY", "KEY", createTableColumnKeyClusterType);
            gb.Prod(createTableColumnConstraintBody).Is("UNIQUE");
            gb.Prod(createTableColumnConstraintBody).Is("UNIQUE", createTableColumnKeyClusterType);
            gb.Prod(createTableColumnConstraintBody).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableColumnConstraintBody).Is("FOREIGN", "KEY", "REFERENCES", qualifiedName);
            gb.Prod(createTableColumnConstraintBody).Is("FOREIGN", "KEY", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableColumnConstraintBody).Is("REFERENCES", qualifiedName);
            gb.Prod(createTableColumnConstraintBody).Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableColumnConstraintBody).Is("DEFAULT", expression);
            gb.Prod(createTableColumnConstraintBody).Is("DEFAULT", "(", expression, ")");

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
            gb.Prod(createTableTableConstraintBody).Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName);
            gb.Prod(createTableTableConstraintBody).Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableTableConstraintBody).Is("DEFAULT", expression, "FOR", identifierTerm);
            gb.Prod(createTableTableConstraintBody).Is("DEFAULT", "(", expression, ")", "FOR", identifierTerm);

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

            gb.Prod(createTableKeyColumnList).Is(createTableKeyColumn);
            gb.Prod(createTableKeyColumnList).Is(createTableKeyColumnList, ",", createTableKeyColumn);
            gb.Prod(createTableKeyColumn).Is(identifierTerm);
            gb.Prod(createTableKeyColumn).Is(identifierTerm, "ASC");
            gb.Prod(createTableKeyColumn).Is(identifierTerm, "DESC");

            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE", createIndexWithClause);

            gb.Prod(createTablePeriodClause).Is("PERIOD", forSystemTimeStart, "SYSTEM_TIME", "(", identifierTerm, ",", identifierTerm, ")");
            gb.Prod(createTableOptions).Is("WITH", "(", createTableOptionList, ")");
            gb.Prod(createTableOptionList).Is(createTableOption);
            gb.Prod(createTableOptionList).Is(createTableOptionList, ",", createTableOption);
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
            gb.Prod(alterTableAddItemList).Is(alterTableAddItem);
            gb.Prod(alterTableAddItemList).Is(alterTableAddItemList, ",", alterTableAddItem);
            gb.Prod(alterTableAddItem).Is(createTableColumnDefinition);
            gb.Prod(alterTableAddItem).Is(createTableConstraint);
            gb.Prod(alterTableAddItem).Is(createTableTableIndex);
            gb.Prod(alterTableAddItem).Is(createTableComputedColumn);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, typeSpec);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, typeSpec, alterTableColumnOptionList);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, "ADD", alterTableColumnOptionList);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, "DROP", alterTableColumnOptionList);
            gb.Prod(alterTableColumnOptionList).Is(alterTableColumnOption);
            gb.Prod(alterTableColumnOptionList).Is(alterTableColumnOptionList, alterTableColumnOption);
            gb.Prod(alterTableColumnOption).Is(createTableColumnOption);
            gb.Prod(alterTableDropItemList).Is(alterTableDropItem);
            gb.Prod(alterTableDropItemList).Is(alterTableDropItemList, ",", alterTableDropItem);
            gb.Prod(alterTableDropItem).Is("COLUMN", identifierTerm);
            gb.Prod(alterTableDropItem).Is("CONSTRAINT", identifierTerm);
            gb.Prod(alterTableDropItem).Is("CONSTRAINT", "ALL");
            gb.Rule(alterTableCheckMode).Keywords("CHECK", "NOCHECK");
            gb.Rule(alterTableConstraintTarget)
                .CanBe(identifierTerm)
                .OrKeywords("ALL");
            gb.Rule(alterTableTriggerTarget)
                .CanBe(identifierTerm)
                .OrKeywords("ALL");

            gb.Prod(createKeyListIndexHead).Is("CREATE", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "NONCLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod(createKeylessIndexHead).Is("CREATE", "CLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod(createIndexStatement).Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")");
            gb.Prod(createIndexStatement).Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")", createIndexTailClauseList);
            gb.Prod(createIndexStatement).Is(createKeylessIndexHead, "ON", qualifiedName);
            gb.Prod(createIndexStatement).Is(createKeylessIndexHead, "ON", qualifiedName, createIndexTailClauseList);

            gb.Prod(createIndexKeyList).Is(createIndexKeyItem);
            gb.Prod(createIndexKeyList).Is(createIndexKeyList, ",", createIndexKeyItem);
            gb.Prod(createIndexKeyItem).Is(identifierTerm);
            gb.Prod(createIndexKeyItem).Is(identifierTerm, "ASC");
            gb.Prod(createIndexKeyItem).Is(identifierTerm, "DESC");

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
            gb.Prod(createIndexIncludeList).Is(identifierTerm);
            gb.Prod(createIndexIncludeList).Is(createIndexIncludeList, ",", identifierTerm);
            gb.Prod(createIndexWhereClause).Is("WHERE", searchCondition);
            gb.Prod(createIndexWithClause).Is("WITH", "(", indexOptionList, ")");
            gb.Prod(createIndexStorageClause).Is("ON", indexStorageTarget);
            gb.Prod(createIndexFileStreamClause).Is("FILESTREAM_ON", indexFileStreamTarget);

            gb.Prod(indexStorageTarget).Is(qualifiedName);
            gb.Prod(indexStorageTarget).Is("DEFAULT");
            gb.Prod(indexStorageTarget).Is(qualifiedName, "(", identifierTerm, ")");
            gb.Prod(indexFileStreamTarget).Is(qualifiedName);
            gb.Prod(indexFileStreamTarget).Is("NULL");
            gb.Prod(indexFileStreamTarget).Is(qualifiedName, "(", identifierTerm, ")");

            gb.Prod(indexOptionList).Is(indexOption);
            gb.Prod(indexOptionList).Is(indexOptionList, ",", indexOption);
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
            gb.Prod(indexOptionValue).Is("NONE");
            gb.Prod(indexOptionValue).Is("SELF");
            gb.Prod(indexOptionValue).Is("BLOCKERS");
            gb.Prod(indexOptionValue).Is("ROW");
            gb.Prod(indexOptionValue).Is("PAGE");
            gb.Prod(indexOptionValue).Is("COLUMNSTORE");
            gb.Prod(indexOptionValue).Is("COLUMNSTORE_ARCHIVE");
            gb.Prod(indexOptionValue).Is(expression, "MINUTES");
            gb.Prod(indexOptionValue).Is(indexOnOffValue, "(", indexOptionList, ")");
            gb.Prod(indexOptionValue).Is("(", indexOptionList, ")");
            gb.Rule(indexOnOffValue).Keywords("ON", "OFF");

            gb.Prod(namedOptionValue).Is(expression);
            gb.Prod(namedOptionValue).Is(identifierTerm);
            gb.Prod(namedOptionValue).Is(qualifiedName);
            gb.Prod(namedOptionValue).Is("ON");
            gb.Prod(namedOptionValue).Is("OFF");

            gb.Prod(maskingOptionList).Is("FUNCTION", "=", expression);

            gb.Prod(encryptionOptionList).Is("COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is(encryptionOptionList, ",", "ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is(encryptionOptionList, ",", "ALGORITHM", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is(encryptionOptionList, ",", "COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is("ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod(encryptionOptionList).Is("ALGORITHM", "=", namedOptionValue);

            gb.Prod(indexPartitionList).Is(indexPartitionItem);
            gb.Prod(indexPartitionList).Is(indexPartitionList, ",", indexPartitionItem);
            gb.Prod(indexPartitionItem).Is(expression);
            gb.Prod(indexPartitionItem).Is(expression, "TO", expression);
            gb.Prod(indexPartitionItem).Is("ALL");

            gb.Prod(alterIndexStatement).Is("ALTER", "INDEX", alterIndexTarget, "ON", qualifiedName, alterIndexAction);
            gb.Rule(alterIndexTarget)
                .CanBe(identifierTerm)
                .Or(qualifiedName)
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
            gb.Prod(alterIndexPartitionSelector).Is("ALL");
        }

        public static void BuildDatabaseGrammar(MsSqlGrammarContext context, MsSqlSchemaDdlSymbols symbols)
        {
            var gb = context.Gb;
            var identifierTerm = context.Symbols.IdentifierTerm;
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;

            var createDatabaseStatement = symbols.CreateDatabaseStatement;
            var createDatabaseClauseList = symbols.CreateDatabaseClauseList;
            var createDatabaseClause = symbols.CreateDatabaseClause;
            var createDatabaseContainmentClause = symbols.CreateDatabaseContainmentClause;
            var createDatabaseOnClause = symbols.CreateDatabaseOnClause;
            var createDatabaseOnFilespecSequence = symbols.CreateDatabaseOnFilespecSequence;
            var createDatabaseFilespec = symbols.CreateDatabaseFilespec;
            var createDatabaseFilegroup = symbols.CreateDatabaseFilegroup;
            var createDatabaseFilespecList = symbols.CreateDatabaseFilespecList;
            var createDatabaseCollateClause = symbols.CreateDatabaseCollateClause;
            var createDatabaseWithClause = symbols.CreateDatabaseWithClause;
            var createDatabaseOptionList = symbols.CreateDatabaseOptionList;
            var createDatabaseOption = symbols.CreateDatabaseOption;
            var createDatabaseOptionValue = symbols.CreateDatabaseOptionValue;
            var createDatabaseOnOffValue = symbols.CreateDatabaseOnOffValue;
            var createDatabaseFilestreamOptionList = symbols.CreateDatabaseFilestreamOptionList;
            var createDatabaseFilestreamOption = symbols.CreateDatabaseFilestreamOption;
            var createDatabaseNonTransactedAccessValue = symbols.CreateDatabaseNonTransactedAccessValue;
            var createDatabaseFileName = symbols.CreateDatabaseFileName;
            var createDatabaseFilespecOptionList = symbols.CreateDatabaseFilespecOptionList;
            var createDatabaseFilespecOption = symbols.CreateDatabaseFilespecOption;
            var createDatabaseSizeSpec = symbols.CreateDatabaseSizeSpec;
            var createDatabaseMaxSizeSpec = symbols.CreateDatabaseMaxSizeSpec;
            var createDatabaseGrowthSpec = symbols.CreateDatabaseGrowthSpec;
            var createDatabaseSizeUnit = symbols.CreateDatabaseSizeUnit;
            var createDatabaseGrowthUnit = symbols.CreateDatabaseGrowthUnit;
            var createDatabaseOnClauseOpt = gb.NT("CreateDatabaseOnClauseOpt");
            var createDatabaseCollateClauseOpt = gb.NT("CreateDatabaseCollateClauseOpt");
            var createDatabaseWithClauseOpt = gb.NT("CreateDatabaseWithClauseOpt");

            gb.Prod(createDatabaseStatement).Is("CREATE", "DATABASE", identifierTerm);
            gb.Prod(createDatabaseStatement).Is("CREATE", "DATABASE", identifierTerm, createDatabaseClauseList);
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

            gb.Prod(createDatabaseFilespecList).Is(createDatabaseFilespec);
            gb.Prod(createDatabaseFilespecList).Is(createDatabaseFilespecList, ",", createDatabaseFilespec);

            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", identifierTerm, createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", identifierTerm, "DEFAULT", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", identifierTerm, "CONTAINS", "FILESTREAM", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", identifierTerm, "CONTAINS", "FILESTREAM", "DEFAULT", createDatabaseOnFilespecSequence);
            gb.Prod(createDatabaseFilegroup).Is("FILEGROUP", identifierTerm, "CONTAINS", "MEMORY_OPTIMIZED_DATA", createDatabaseOnFilespecSequence);

            gb.Prod(createDatabaseCollateClause).Is("COLLATE", identifierTerm);

            gb.Prod(createDatabaseWithClause).Is("WITH", createDatabaseOptionList);
            gb.Prod(createDatabaseOptionList).Is(createDatabaseOption);
            gb.Prod(createDatabaseOptionList).Is(createDatabaseOptionList, ",", createDatabaseOption);

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
            gb.Prod(createDatabaseOptionValue).Is(identifierTerm);
            gb.Prod(createDatabaseOptionValue).Is(stringLiteral);

            gb.Rule(createDatabaseOnOffValue).Keywords("ON", "OFF");

            gb.Prod(createDatabaseFilestreamOptionList).Is(createDatabaseFilestreamOption);
            gb.Prod(createDatabaseFilestreamOptionList).Is(createDatabaseFilestreamOptionList, ",", createDatabaseFilestreamOption);
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

            gb.Prod(createDatabaseFileName).Is(identifierTerm);
            gb.Prod(createDatabaseFileName).Is(stringLiteral);

            gb.Prod(createDatabaseFilespecOptionList).Is(createDatabaseFilespecOption);
            gb.Prod(createDatabaseFilespecOptionList).Is(createDatabaseFilespecOptionList, ",", createDatabaseFilespecOption);
            gb.Prod(createDatabaseFilespecOption).Is("SIZE", "=", createDatabaseSizeSpec);
            gb.Prod(createDatabaseFilespecOption).Is("MAXSIZE", "=", createDatabaseMaxSizeSpec);
            gb.Prod(createDatabaseFilespecOption).Is("FILEGROWTH", "=", createDatabaseGrowthSpec);

            gb.Prod(createDatabaseSizeSpec).Is(number);
            gb.Prod(createDatabaseSizeSpec).Is(number, createDatabaseSizeUnit);
            gb.Prod(createDatabaseSizeSpec).Is(number, identifierTerm);

            gb.Prod(createDatabaseMaxSizeSpec).Is("UNLIMITED");
            gb.Prod(createDatabaseMaxSizeSpec).Is(number);
            gb.Prod(createDatabaseMaxSizeSpec).Is(number, createDatabaseSizeUnit);
            gb.Prod(createDatabaseMaxSizeSpec).Is(number, identifierTerm);

            gb.Prod(createDatabaseGrowthSpec).Is(number);
            gb.Prod(createDatabaseGrowthSpec).Is(number, createDatabaseGrowthUnit);
            gb.Prod(createDatabaseGrowthSpec).Is(number, identifierTerm);

            gb.Rule(createDatabaseSizeUnit).Keywords("KB", "MB", "GB", "TB");

            gb.Prod(createDatabaseGrowthUnit).Is(createDatabaseSizeUnit);
            gb.Prod(createDatabaseGrowthUnit).Is("%");
        }
    }
}
