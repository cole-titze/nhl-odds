import pytds


def get_connection(config: dict) -> pytds.Connection:
    return pytds.connect(
        server=config["server"],
        database=config["database"],
        user=config["user"],
        password=config["password"],
    )
