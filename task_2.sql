CREATE
OR REPLACE FUNCTION get_client_daily_payments (clientId BIGINT, startedAt DATE, endedAt DATE) RETURNS TABLE (dt DATE, amount money) LANGUAGE sql AS $$
WITH
 payments AS (
  SELECT
   date_trunc('day', "Dt")::date AS dt,
   SUM("Amount") AS amount
  FROM
   "ClientPayments"
  WHERE
   "ClientId" = clientId
   AND "Dt" >= startedAt
   AND "Dt" < endedAt + INTERVAL '1 day'
  GROUP BY
   1
 )
SELECT
 d.dt,
 COALESCE(p.amount, 0::money)
FROM
 generate_series(startedAt, endedAt, '1 day') d (dt)
 LEFT JOIN payments p USING (dt);
$$;