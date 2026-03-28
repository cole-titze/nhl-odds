import psycopg2


def get_connection(config: dict):
    return psycopg2.connect(
        host=config["host"],
        dbname=config["dbname"],
        user=config["user"],
        password=config["password"],
    )
