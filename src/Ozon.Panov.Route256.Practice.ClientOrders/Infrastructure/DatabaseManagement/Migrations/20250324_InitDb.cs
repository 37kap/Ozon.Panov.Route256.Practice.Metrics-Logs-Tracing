using FluentMigrator;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement.Migrations;

[Migration(20250324)]
public class InitDb : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS outbox_messages (
                id UUID PRIMARY KEY,
                topic VARCHAR NOT NULL,
                key VARCHAR NOT NULL,
                value TEXT NOT NULL,
                created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL,
                is_processed BOOLEAN NOT NULL DEFAULT FALSE,
                processed_at TIMESTAMP WITHOUT TIME ZONE
            );
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_outbox_messages_is_processed ON outbox_messages (is_processed);
        ");

        Execute.Sql(@"
            CREATE TABLE client_orders (
                order_id BIGINT PRIMARY KEY,
                comment text NOT NULL,
                customer_id BIGINT NOT NULL,
                status INT NOT NULL,
                created_at TIMESTAMP NOT NULL
            );
        ");

        Execute.Sql("CREATE INDEX idx_client_orders_customer_id ON client_orders (customer_id);");
        Execute.Sql("CREATE UNIQUE INDEX idx_client_orders_comment_unique ON client_orders (comment);");
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS idx_client_orders_comment_unique;");
        Execute.Sql("DROP INDEX IF EXISTS idx_client_orders_customer_id;");
        Execute.Sql("DROP TABLE IF EXISTS client_orders;");

        Execute.Sql(@"
            DROP INDEX IF NOT EXISTS idx_outbox_messages_is_processed 
            ON outbox_messages (is_processed);");
        Execute.Sql("DROP TABLE IF EXISTS outbox_messages;");
    }
}