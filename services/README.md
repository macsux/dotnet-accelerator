A collection of services to setup up local development environment via docker-compose. 

#### How to use

##### Start

```
docker-compose up -d <SERVICENAME...>
```

*You can specify more then one service name. Omit to start everything*

##### Stop

```
docker-compose down
```

#### Included services:

- `config-server` - [Spring Cloud Config Server](https://cloud.spring.io/spring-cloud-config) 
  - http://localhost:8888
  - By default looks for config files inside `./config` directory. Docker-compose file has commented settings to switch over to Git based configuration
- `eureka` - [Spring Cloud Eureka](https://spring.io/projects/spring-cloud-netflix) 
  - http://localhost:8761
- `rabbitmq` - [RabbitMQ](https://www.rabbitmq.com/)
  - Server: localhost:5672
  - Credentials: guest/guest
  - Management UI: http://localhost:8084
- `mysql` - MySQL
  - Server: localhost:3306
  - Credentials: root / (blank)
- `phpmyadmin` - [PhpMyAdmin](https://www.phpmyadmin.net/), UI to interact with `mysql` container
  - UI: http://localhost:8083
- `zipkin` - Zipkin - distributed tracing
  - http://localhost:9411
- `omnidb` - OmniDB - multi-db web based GUI (postgres, mysql)
  - http://localhost:8085
  - Credentials: admin/admin
  - Note: when connecting to servers, use container service name not localhost
- `postgres` - PostgreSQL
  - Server: localhost:5432
  - Credentials: admin/admin