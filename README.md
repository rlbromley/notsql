notsql
======

A NoSQL layer (in the form of a JSON document store) designed to sit on top of Microsoft's SQL Server (or standalone MDF databases).  It's safe to say this is still in the early stages of development.

quick usage
===========
* JSON documents are stored in tables via write().  Write() returns the document plus new keys _id and _rev.  _id is the document's unique id and _rev is the documents revision id.  Write() is also used to update an existing record; there is a rule requiring that the revision id in the document being updated match the database's value.
* A JSON document can be retrieved from a table via read().  Read() expects a JSON object in the format

        { "_id": "73a69256-3c3e-4934-8af1-37ae3a9833fe" } 

    and will return either an empty JObject or the requested document.  If a document is found, the act of read()'ing it will change its _rev id.
* There is also a rudimentary querying mechanism available through find().  Find works on JSON objects of the format

        { object property : { operator : value } }
        
    For example, to search through documents for an age property that equals 21, the JSON would be
    
        { "age" : { "$eq" : 21 } }
    
    The defined operations are
    
        $eq - equals
        $lt - less than
        $gt - greater than
        $lte - less than or equal to
        $gte - greater than or equal to

todo
====

- flesh out the unit tests
- http and/or other means of network access
- improve query abilities; find() has obvious limitations

author
======
I wanted to learn some more about nosql databases (mostly the ideas and how they change web development), so I'm writing one.

Interested in showing support?  Feel free to help the code along yourself.  Additionally I'm not above donations, or being bribed to stop this.  Interested parties can send Dogecoin here: 
    
	DPc73geE3Rpz96MnQoXBF3RQpucbCSR2Sm
