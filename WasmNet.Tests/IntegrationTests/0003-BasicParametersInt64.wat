;; invoke: add (i64:3000000000) (i64:5000000000)
;; expect: (i64:8000000000)

(module
    (func (export "add") (param $x i64) (param $y i64) (result i64)
        local.get $x
        local.get $y
        i64.add
    )
)