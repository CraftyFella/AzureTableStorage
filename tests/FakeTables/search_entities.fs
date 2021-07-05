module search_entities

open Expecto
open Microsoft.Azure.Cosmos.Table
open System

[<Tests>]
let searchTests =
  testList
    "search_entities"
    [ test "all with matching partition key" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1"; "rk2"; "rk4"; "rk5" ] "unexpected row Keys"

        for result in results do
          Expect.isNotNull result.ETag "eTag is expected"

          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "all rows" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let query = TableQuery<DynamicTableEntity>()

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1"; "rk2"; "rk3"; "rk4"; "rk5" ] "unexpected row Keys"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"
      }

      test "all with matching row key" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk2" "rk2"
          createEntity "pk3" "rk1"
          createEntity "pk4" "rk4"
          createEntity "pk5" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk1")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey)
          |> Seq.toList

        Expect.equal partitionKeys [ "pk1"; "pk3" ] "unexpected partition Keys"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "partionKey OR rowKey" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk2" "rk2"
          createEntity "pk3" "rk3"
          createEntity "pk4" "rk4"
          createEntity "pk5" "rk5" ]
        |> List.iter insert

        let partitionKeyFilter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk3")

        let rowKeyFilter =
          TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk5")

        let filter =
          TableQuery.CombineFilters(partitionKeyFilter, TableOperators.Or, rowKeyFilter)

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk3", "rk3"; "pk5", "rk5" ] "unexpected rows"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "partionKey AND rowKey" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk2" "rk3"
          createEntity "pk3" "rk3"
          createEntity "pk3" "rk4"
          createEntity "pk5" "rk5" ]
        |> List.iter insert

        let partitionKeyFilter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk3")

        let rowKeyFilter =
          TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk3")

        let filter =
          TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, rowKeyFilter)

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk3", "rk3" ] "unexpected rows"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "property String search" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntityWithString "pk1" "rk1" "One"
          createEntityWithString "pk2" "rk3" "Two"
          createEntityWithString "pk3" "rk3" "One"
          createEntityWithString "pk3" "rk4" "Two"
          createEntityWithString "pk5" "rk5" "Three" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("StringField", QueryComparisons.Equal, "Two")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk2", "rk3"; "pk3", "rk4" ] "unexpected rows"

        for result in results do
          Expect.equal (result.Properties.["StringField"].StringValue) "Two" "unexpected values"

      }

      test "property Int search" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntityWithInt "pk1" "rk1" 1
          createEntityWithInt "pk2" "rk3" 2
          createEntityWithInt "pk3" "rk3" 1
          createEntityWithInt "pk3" "rk4" 2
          createEntityWithInt "pk5" "rk5" 3 ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterConditionForInt("IntField", QueryComparisons.Equal, 2)

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk2", "rk3"; "pk3", "rk4" ] "unexpected rows"

        for result in results do
          Expect.equal (result.Properties.["IntField"].Int32Value.Value) 2 "unexpected values"

      }

      test "no filter" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "bbb"
          createEntity "pk1" "aaa"
          createEntity "pk2" "ccc"
          createEntity "pk1" "eee"
          createEntity "pk1" "ddd" ]
        |> List.iter insert

        let query = TableQuery<DynamicTableEntity>()

        let results = executeQuery table query

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "aaa"; "bbb"; "ccc"; "ddd"; "eee" ] "unexpected row Keys"
      }

      test "no matches" {
        let table = createLocalTables ()

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        Expect.equal (results |> Seq.length) 0 "unexpected length"

      }

      test "Take 1 row" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>()
            .Where(filter)
            .Take(Nullable 1)

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1" ] "unexpected row Keys"

        for result in results do
          Expect.isNotNull result.ETag "eTag is expected"

          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "Take more than rows in db" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let query =
          TableQuery<DynamicTableEntity>()
            .Take(Nullable 100)

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let actualCount = results |> Seq.length

        Expect.equal actualCount 5 "all 5 rows expected"

      }

      test "Select single field" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>()
            .Where(filter)
            .Take(Nullable 1)
            .Select([ "StringField" ] |> ResizeArray)

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1" ] "unexpected row Keys"

        for result in results do
          Expect.isNotNull result.ETag "eTag is expected"
          Expect.equal (result.Properties |> Seq.length) 1 "unexpected values"
          Expect.equal (result.Properties.["StringField"].StringValue) "StringValue" "unexpected values"

      }

      test "Select empty" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>()
            .Where(filter)
            .Take(Nullable 1)
            .Select(ResizeArray())

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1" ] "unexpected row Keys"

        for result in results do
          Expect.isNotNull result.ETag "eTag is expected"
          Expect.equal (result.Properties |> Seq.length) 8 "unexpected values"

      }

      test "Paging" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>()
            .Where(filter)
            .Take(Nullable 1)

        let results = executeQuery table query

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1"; "rk2"; "rk4"; "rk5" ] "unexpected row Keys"
      }

      test "Paging non sequential row keys" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "bbb"
          createEntity "pk1" "aaa"
          createEntity "pk2" "ccc"
          createEntity "pk1" "eee"
          createEntity "pk1" "ddd" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>()
            .Where(filter)
            .Take(Nullable 1)

        let results = executeQuery table query

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "aaa"; "bbb"; "ddd"; "eee" ] "unexpected row Keys"
      }

      test "Paging no filter" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "bbb"
          createEntity "pk1" "aaa"
          createEntity "pk2" "ccc"
          createEntity "pk1" "eee"
          createEntity "pk1" "ddd" ]
        |> List.iter insert

        let query =
          TableQuery<DynamicTableEntity>().Take(Nullable 1)

        let results = executeQuery table query

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "aaa"; "bbb"; "ccc"; "ddd"; "eee" ] "unexpected row Keys"
      }

      test "Paging size 2 no filter" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "bbb"
          createEntity "pk1" "aaa"
          createEntity "pk2" "ccc"
          createEntity "pk1" "eee"
          createEntity "pk1" "ddd" ]
        |> List.iter insert

        let query =
          TableQuery<DynamicTableEntity>().Take(Nullable 2)

        let results = executeQuery table query

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "aaa"; "bbb"; "ccc"; "ddd"; "eee" ] "unexpected row Keys"
      }

      test "Paging mixture of cases" {
        let table = createLocalTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "PK1" "bbb"
          createEntity "pk1" "aaa"
          createEntity "pk2" "ccc"
          createEntity "PK1" "EEE"
          createEntity "pk1" "ddd" ]
        |> List.iter insert

        let query =
          TableQuery<DynamicTableEntity>().Take(Nullable 2)

        let results = executeQuery table query

        let rowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal
          rowKeys
          [ "PK1", "EEE"
            "PK1", "bbb"
            "pk1", "aaa"
            "pk1", "ddd"
            "pk2", "ccc" ]
          "unexpected row Keys"
      } ]
