;; invoke: mul (i64:2) (i64:3)
;; expect: (i64:6)

(module
    (func (export "mul") (param $x i64) (param $y i64) (result i64)
        local.get $x
        local.get $y
        i64.mul
    )
)