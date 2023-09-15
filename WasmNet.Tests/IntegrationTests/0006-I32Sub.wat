;; invoke: sub (i32:3) (i32:1)
;; expect: (i32:2)

(module
    (func (export "sub") (param $x i32) (param $y i32) (result i32)
        local.get $x
        local.get $y
        i32.sub
    )
)