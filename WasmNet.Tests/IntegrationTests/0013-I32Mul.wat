;; invoke: mul (i32:2) (i32:3)
;; expect: (i32:6)

(module
    (func (export "mul") (param $x i32) (param $y i32) (result i32)
        local.get $x
        local.get $y
        i32.mul
    )
)