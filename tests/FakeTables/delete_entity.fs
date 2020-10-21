module delete_entity

open Expecto
open System
open Host
open Microsoft.Azure.Cosmos.Table


[<Tests>]
let deleteTests =
  testList
    "delete_entity"
    [ test "entity exists" {
        let table = createFakeTables ()

        let entity =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())

        entity
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let actual =
          TableOperation.Delete(entity) |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 404 "unexpected result"

      }
      test "entity doesnt exist" {
        let table = createFakeTables ()

        let entity =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())

        let run () =
          TableOperation.Delete(entity)
          |> table.Execute
          |> ignore

        Expect.throwsT<Microsoft.Azure.Cosmos.Table.StorageException> run "expected exception"

      }
      test "row exists and correct etag used is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        Expect.isNotNull (actual.Etag) "eTag is expected"

        let actual =
          DynamicTableEntity("pk2", "r2k", actual.Etag, stringFieldType "Updated Value")
          |> TableOperation.Delete
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
      }

      test "row exists and old etag used is rejected" {
        let table = createFakeTables ()

        let oldEtag = "W/\"datetime'2020-10-16T10:37:44Z'\""

        let _ =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        let run () =
          DynamicTableEntity("pk2", "r2k", oldEtag, stringFieldType "Updated Value")
          |> TableOperation.Delete
          |> table.Execute
          |> ignore

        Expect.throwsT<Microsoft.Azure.Cosmos.Table.StorageException> run "expected exception"

      }

      test "row exists and wildcard (*) etag used is accepted" {
        let table = createFakeTables ()

        let wildcardEtag = "*"

        let _ =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          DynamicTableEntity("pk2", "r2k", wildcardEtag, stringFieldType "Updated Value")
          |> TableOperation.Delete
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]
