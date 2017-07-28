# Orleans Indexing

> This project is has undergone a modest amount of testing and code review, and is NOT READY for production use. It is published to collect community feedback and attract others to make it production-ready. 

Enable grains to be indexed and queried by scalar properties. A [research paper](http://cidrdb.org/cidr2017/papers/p29-bernstein-cidr17.pdf) describing the interface and implementation was published at [CIDR 2017] (http://cidrdb.org/cidr2017/index.html).

## Features

- Index over all grains of a class 
- Index over only the activated grains of a class
- Fault-tolerant multi-step workflow for index update
- Index stored as a single grain
- Index partitioned by key value
- Index over active grains physically-partitioned by grain location
- Index with very large buckets due to skew

## Source Code

See src/OrleansIndexing  and test/Tester/IndexingTests

## Example Usage

> TBD

See also test/Tester/IndexingTests

## To Do

- Range indexes
- Replace workflows by transactions

## License

- [M.I.T.](https://github.com/dotnet/orleans/blob/master/LICENSE)




