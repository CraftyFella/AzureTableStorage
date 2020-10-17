module get_entity

open Expecto
open System
open Host
open Microsoft.Azure.Cosmos.Table

[<Tests>]
let get_entity =
  testList
    "get_entity"
    [ test "entity exists" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()
        let insertResult =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 200 "unexpected result"

        let result =
          actual.Result |> unbox<DynamicTableEntity>

        Expect.equal (result.PartitionKey) "pk2" "unexpected value"
        Expect.equal (result.RowKey) "r2k" "unexpected value"
        Expect.isNotNull (actual.Etag) "eTag is expected"

        for field in fields do
          Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }
      test "entity doesnt exist" {
        let table = createFakeTables ()

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 404 "unexpected result"

      } ]