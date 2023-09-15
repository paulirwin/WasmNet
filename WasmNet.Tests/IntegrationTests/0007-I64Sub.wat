;; invoke: sub (i64:5000000000) (i64:2000000000)
;; expect: (i64:3000000000)

(module
    (func (export "sub") (param $x i64) (param $y i64) (result i64)
        local.get $x
        local.get $y
        i64.sub
    )
)