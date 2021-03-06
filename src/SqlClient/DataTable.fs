﻿namespace FSharp.Data

open System
open System.Data
open System.Data.SqlClient
open System.Collections.Generic

open FSharp.Data.SqlClient

[<Sealed>]
///<summary>Generic implementation of <see cref='DataTable'/></summary>
type DataTable<'T when 'T :> DataRow>(tableName, selectCommand) = 
    inherit DataTable(tableName)

    let rows = base.Rows

    member __.Rows : IList<'T> = {
        new IList<'T> with
            member __.GetEnumerator() = rows.GetEnumerator()
            member __.GetEnumerator() : IEnumerator<'T> = (Seq.cast<'T> rows).GetEnumerator() 

            member __.Count = rows.Count
            member __.IsReadOnly = rows.IsReadOnly
            member __.Item 
                with get index = downcast rows.[index]
                and set index row = 
                    rows.RemoveAt(index)
                    rows.InsertAt(row, index)

            member __.Add row = rows.Add row
            member __.Clear() = rows.Clear()
            member __.Contains row = rows.Contains row
            member __.CopyTo(dest, index) = rows.CopyTo(dest, index)
            member __.IndexOf row = rows.IndexOf row
            member __.Insert(index, row) = rows.InsertAt(row, index)
            member __.Remove row = rows.Remove(row); true
            member __.RemoveAt index = rows.RemoveAt(index)
    }

    member __.NewRow(): 'T = downcast base.NewRow()

    member this.Update(?connection, ?transaction, ?batchSize) = 
        
        use dataAdapter = new SqlDataAdapter(selectCommand)
        use commandBuilder = new SqlCommandBuilder(dataAdapter) 

        connection |> Option.iter dataAdapter.SelectCommand.set_Connection
        transaction |> Option.iter dataAdapter.SelectCommand.set_Transaction
        batchSize |> Option.iter dataAdapter.set_UpdateBatchSize

        dataAdapter.Update(this)

    member this.BulkCopy(?connection, ?copyOptions, ?transaction, ?batchSize, ?timeout) = 
        
        let connection = defaultArg connection selectCommand.Connection
        use __ = connection.UseLocally()
        use bulkCopy = 
            new SqlBulkCopy(
                connection, 
                copyOptions = defaultArg copyOptions SqlBulkCopyOptions.Default, 
                externalTransaction = defaultArg transaction selectCommand.Transaction
            )
        bulkCopy.DestinationTableName <- tableName
        batchSize |> Option.iter bulkCopy.set_BatchSize
        timeout |> Option.iter bulkCopy.set_BulkCopyTimeout
        bulkCopy.WriteToServer this


