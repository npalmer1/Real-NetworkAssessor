$TTL	6048000
$ORIGIN abc.com.
@	IN	SOA	ns1	root	(
		1		;Serial
		604800		;Refresh
		86400		;Retry
		2419200		;Expire
		604800		;Miniumum
	)

@	IN	A	192.168.1.123
@	IN	NS	ns1
ns1	IN	A	192.168.1.123
work	IN	A	192.168.1.124


