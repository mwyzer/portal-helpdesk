FROM postgres:17

RUN apt-get update && apt-get install -y --no-install-recommends \
    postgresql-server-dev-17 \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

ADD https://github.com/pgvector/pgvector/archive/refs/tags/v0.8.0.tar.gz /tmp/pgvector.tar.gz
RUN cd /tmp && tar xzf pgvector.tar.gz && cd pgvector-0.8.0 \
    && make && make install \
    && rm -rf /tmp/pgvector*

RUN apt-get purge -y postgresql-server-dev-17 build-essential \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/*
